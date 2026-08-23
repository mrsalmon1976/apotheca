using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Users.GetUserWorkspaces;

[Route("users/me/workspaces")]
public class GetUserWorkspacesController(
    IDbContextFactory dbContextFactory,
    GetUserWorkspacesRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetUserWorkspaces(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeAccessAsync(db, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var uid        = securityResult.FirebaseUid;
        var workspaces = await repo.GetWorkspacesByUidAsync(db, uid);
        var stats      = await repo.GetWorkspaceStatsAsync(db, uid);

        var statsById = stats.ToDictionary(s => s.WorkspaceId);
        return Ok(workspaces.ToResponse(statsById));
    }
}
