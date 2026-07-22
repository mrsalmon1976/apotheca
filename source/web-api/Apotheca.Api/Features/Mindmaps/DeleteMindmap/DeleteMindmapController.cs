using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Mindmaps.DeleteMindmap;

[Route("projects/{projectId}/mindmaps")]
public class DeleteMindmapController(
    IDbContextFactory dbContextFactory,
    DeleteMindmapRepository repo,
    ISecurityProvider securityProvider,
    ILogger<DeleteMindmapController> logger) : AuthenticatedBaseController
{
    [HttpDelete("{mindmapId}")]
    public async Task<IActionResult> DeleteMindmap(
        string projectId,
        string mindmapId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var exists = await repo.MindmapExistsAsync(db, projectId, mindmapId);
        if (!exists)
            return NotFound();

        await db.BeginTransactionAsync(cancellationToken);

        await repo.SoftDeleteMindmapNodesAsync(db, mindmapId);
        await repo.SoftDeleteMindmapAsync(db, mindmapId);

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Mindmap deleted. MindmapId: {MindmapId}, ProjectId: {ProjectId}, UserId: {UserId}", mindmapId, projectId, securityResult.UserId);

        return NoContent();
    }
}
