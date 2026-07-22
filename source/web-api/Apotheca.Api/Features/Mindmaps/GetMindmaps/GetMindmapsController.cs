using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Mindmaps.GetMindmaps;

[Route("projects/{projectId}/mindmaps")]
public class GetMindmapsController(
    IDbContextFactory dbContextFactory,
    GetMindmapsRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetMindmaps(
        string projectId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var mindmaps = await repo.GetMindmapsAsync(db, projectId);
        return Ok(mindmaps);
    }
}
