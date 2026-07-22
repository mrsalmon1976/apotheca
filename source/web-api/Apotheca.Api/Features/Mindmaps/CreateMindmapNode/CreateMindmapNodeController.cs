using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Mindmaps.CreateMindmapNode;

[Route("projects/{projectId}/mindmaps/{mindmapId}/nodes")]
public class CreateMindmapNodeController(
    IDbContextFactory dbContextFactory,
    CreateMindmapNodeRepository repo,
    ISecurityProvider securityProvider,
    ILogger<CreateMindmapNodeController> logger) : AuthenticatedBaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateMindmapNode(
        string projectId,
        string mindmapId,
        [FromBody] CreateMindmapNodeRequest request,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var mindmapExists = await repo.MindmapExistsAsync(db, projectId, mindmapId);
        if (!mindmapExists)
            return NotFound(new { error = $"Mindmap '{mindmapId}' was not found." });

        var parentExists = await repo.NodeExistsAsync(db, mindmapId, request.ParentNodeId);
        if (!parentExists)
            return NotFound(new { error = $"Parent node '{request.ParentNodeId}' was not found." });

        await db.BeginTransactionAsync(cancellationToken);

        var header = string.IsNullOrWhiteSpace(request.Header) ? "New Node" : request.Header.Trim();
        var body = request.Body ?? "";
        var position = await repo.GetNextPositionAsync(db, mindmapId, request.ParentNodeId);
        var nodeId = await repo.InsertNodeAsync(db, mindmapId, request.ParentNodeId, securityResult.UserId, header, body, position);

        await repo.RecomputeSearchAsync(db, projectId, mindmapId);
        await repo.TouchMindmapAsync(db, mindmapId);

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Mindmap node created. NodeId: {NodeId}, MindmapId: {MindmapId}, ProjectId: {ProjectId}, UserId: {UserId}", nodeId, mindmapId, projectId, securityResult.UserId);

        return CreatedAtAction(nameof(CreateMindmapNode), new { projectId, mindmapId }, new { id = nodeId });
    }
}
