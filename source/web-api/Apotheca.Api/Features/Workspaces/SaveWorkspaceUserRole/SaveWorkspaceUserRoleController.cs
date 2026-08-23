using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Workspaces.SaveWorkspaceUserRole;

[Route("workspaces/{workspaceId}/users/{userId}")]
public class SaveWorkspaceUserRoleController(
    IDbContextFactory dbContextFactory,
    SaveWorkspaceUserRoleRepository repo,
    SaveWorkspaceUserRoleValidator validator,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpPatch]
    public async Task<IActionResult> SaveWorkspaceUserRole(
        string workspaceId,
        string userId,
        [FromBody] SaveWorkspaceUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = validator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeWorkspaceAccessAsync(db, workspaceId, requireAdmin: true, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var currentRole = await repo.GetMemberRoleAsync(db, workspaceId, userId);
        if (currentRole is null)
            return NotFound(new { error = "That user is not a member of this workspace." });

        if (currentRole == DataConstants.WorkspaceRole.Admin &&
            request.WorkspaceRole != DataConstants.WorkspaceRole.Admin)
        {
            var adminCount = await repo.CountAdminsAsync(db, workspaceId);
            if (adminCount <= 1)
                return BadRequest(new { error = "Cannot demote the last admin of a workspace." });
        }

        await repo.SaveMemberRoleAsync(db, workspaceId, userId, request.WorkspaceRole);

        return Ok();
    }
}
