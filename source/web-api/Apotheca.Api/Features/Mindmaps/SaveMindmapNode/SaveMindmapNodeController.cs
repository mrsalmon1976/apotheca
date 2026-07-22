using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Mindmaps.SaveMindmapNode;

[Route("projects/{projectId}/mindmaps/{mindmapId}/nodes/{nodeId}")]
public class SaveMindmapNodeController(
    IDbContextFactory dbContextFactory,
    SaveMindmapNodeRepository repo,
    SaveMindmapNodeValidator validator,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpPatch]
    public async Task<IActionResult> SaveMindmapNode(
        string projectId,
        string mindmapId,
        string nodeId,
        [FromBody] SaveMindmapNodeRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = validator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var nodeExists = await repo.NodeExistsAsync(db, mindmapId, nodeId);
        if (!nodeExists)
            return NotFound(new { error = $"Node '{nodeId}' was not found." });

        await db.BeginTransactionAsync(cancellationToken);

        await repo.UpdateNodeAsync(db, nodeId, request.Header, request.Body, request.Collapsed);
        await repo.RecomputeSearchAsync(db, projectId, mindmapId);
        await repo.TouchMindmapAsync(db, mindmapId);

        await db.CommitAsync(cancellationToken);

        return Ok();
    }
}
