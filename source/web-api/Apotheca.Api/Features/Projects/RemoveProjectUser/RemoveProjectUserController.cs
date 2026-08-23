using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Projects.RemoveProjectUser;

[Route("projects/{projectId}/users/{userId}")]
public class RemoveProjectUserController(
    IDbContextFactory dbContextFactory,
    RemoveProjectUserRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpDelete]
    public async Task<IActionResult> RemoveProjectUser(
        string projectId,
        string userId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        if (securityResult.Role != DataConstants.ProjectRole.Admin)
            return Unauthorized(new { error = "Only project admins can perform this action." });

        var currentRole = await repo.GetMemberRoleAsync(db, projectId, userId);
        if (currentRole is null)
            return NotFound(new { error = "That user is not a member of this project." });

        if (currentRole == DataConstants.ProjectRole.Admin)
        {
            var adminCount = await repo.CountAdminsAsync(db, projectId);
            if (adminCount <= 1)
                return BadRequest(new { error = "Cannot remove the last admin of a project." });
        }

        await repo.RemoveMemberAsync(db, projectId, userId);

        return Ok();
    }
}
