using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Workspaces.GetWorkspaceUsers;

[Route("workspaces/{workspaceId}/users")]
public class GetWorkspaceUsersController(
    IDbContextFactory dbContextFactory,
    GetWorkspaceUsersRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetWorkspaceUsers(string workspaceId, CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeWorkspaceAccessAsync(db, workspaceId, requireAdmin: false, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var members = await repo.GetMembersAsync(db, workspaceId);
        return Ok(members.ToResponse());
    }
}
