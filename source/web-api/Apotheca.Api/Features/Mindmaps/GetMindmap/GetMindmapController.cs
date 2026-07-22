using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Mindmaps.GetMindmap;

[Route("projects/{projectId}/mindmaps/{mindmapId}")]
public class GetMindmapController(
    IDbContextFactory dbContextFactory,
    GetMindmapRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetMindmap(
        string projectId,
        string mindmapId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var mindmap = await repo.GetMindmapInfoAsync(db, projectId, mindmapId);
        if (mindmap is null)
            return NotFound();

        var nodes = await repo.GetNodesAsync(db, mindmapId);
        return Ok(new { id = mindmap.Id, name = mindmap.Name, nodes });
    }
}
