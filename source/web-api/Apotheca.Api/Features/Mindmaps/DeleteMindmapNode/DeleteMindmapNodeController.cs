using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Mindmaps.DeleteMindmapNode;

[Route("projects/{projectId}/mindmaps/{mindmapId}/nodes")]
public class DeleteMindmapNodeController(
    IDbContextFactory dbContextFactory,
    DeleteMindmapNodeRepository repo,
    ISecurityProvider securityProvider,
    ILogger<DeleteMindmapNodeController> logger) : AuthenticatedBaseController
{
    [HttpDelete("{nodeId}")]
    public async Task<IActionResult> DeleteMindmapNode(
        string projectId,
        string mindmapId,
        string nodeId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var node = await repo.GetNodeInfoAsync(db, mindmapId, nodeId);
        if (node is null)
            return NotFound();

        if (node.ParentNodeId is null)
            return BadRequest(new { error = "The root node cannot be deleted. Delete the mindmap instead." });

        await db.BeginTransactionAsync(cancellationToken);

        await repo.SoftDeleteNodeAndDescendantsAsync(db, mindmapId, nodeId);
        await repo.RecomputeSearchAsync(db, projectId, mindmapId);
        await repo.TouchMindmapAsync(db, mindmapId);

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Mindmap node deleted. NodeId: {NodeId}, MindmapId: {MindmapId}, ProjectId: {ProjectId}, UserId: {UserId}", nodeId, mindmapId, projectId, securityResult.UserId);

        return NoContent();
    }
}
