using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Users.GetUserProjects;

[Route("users/me/projects")]
public class GetUserProjectsController(
    IDbContextFactory dbContextFactory,
    GetUserProjectsRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetUserProjects([FromQuery] string workspaceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
            return BadRequest(new { error = "workspaceId is required." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeAccessAsync(db, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var uid      = securityResult.FirebaseUid;
        var projects = await repo.GetProjectsByUidAsync(db, uid, workspaceId);
        var stats    = await repo.GetProjectStatsAsync(db, uid, workspaceId);

        var statsById = stats.ToDictionary(s => s.ProjectId);
        return Ok(projects.ToResponse(statsById));
    }
}
