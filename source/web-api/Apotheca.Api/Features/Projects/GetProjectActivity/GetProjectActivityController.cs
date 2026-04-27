using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Projects.GetProjectActivity;

[Route("projects/{projectId}/activity")]
public class GetProjectActivityController(
    IDbContextFactory dbContextFactory,
    GetProjectActivityRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetProjectActivity(
        string projectId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var entries = await repo.GetProjectActivityAsync(db, projectId);
        return Ok(entries.ToResponse());
    }
}
