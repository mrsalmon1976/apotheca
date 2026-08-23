using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Workspaces.RemoveWorkspaceUser;

[Route("workspaces/{workspaceId}/users/{userId}")]
public class RemoveWorkspaceUserController(
    IDbContextFactory dbContextFactory,
    RemoveWorkspaceUserRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpDelete]
    public async Task<IActionResult> RemoveWorkspaceUser(
        string workspaceId,
        string userId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeWorkspaceAccessAsync(db, workspaceId, requireAdmin: true, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var currentRole = await repo.GetMemberRoleAsync(db, workspaceId, userId);
        if (currentRole is null)
            return NotFound(new { error = "That user is not a member of this workspace." });

        if (currentRole == DataConstants.WorkspaceRole.Admin)
        {
            var adminCount = await repo.CountAdminsAsync(db, workspaceId);
            if (adminCount <= 1)
                return BadRequest(new { error = "Cannot remove the last admin of a workspace." });
        }

        await db.BeginTransactionAsync(cancellationToken);

        await repo.RemoveProjectAccessForWorkspaceAsync(db, workspaceId, userId);
        await repo.ReassignCurrentWorkspaceAsync(db, userId, workspaceId);
        await repo.RemoveMemberAsync(db, workspaceId, userId);

        await db.CommitAsync(cancellationToken);

        return Ok();
    }
}
