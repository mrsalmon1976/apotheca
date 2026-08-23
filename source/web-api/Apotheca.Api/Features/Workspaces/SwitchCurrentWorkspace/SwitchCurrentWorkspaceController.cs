using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Workspaces.SwitchCurrentWorkspace;

[Route("workspaces/{workspaceId}/current")]
public class SwitchCurrentWorkspaceController(
    IDbContextFactory dbContextFactory,
    SwitchCurrentWorkspaceRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpPatch]
    public async Task<IActionResult> SwitchCurrentWorkspace(string workspaceId, CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeWorkspaceAccessAsync(db, workspaceId, requireAdmin: false, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        await repo.SetCurrentWorkspaceAsync(db, securityResult.UserId, workspaceId);

        return Ok();
    }
}
