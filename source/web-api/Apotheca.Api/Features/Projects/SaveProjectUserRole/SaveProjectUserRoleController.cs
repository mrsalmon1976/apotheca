using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Projects.SaveProjectUserRole;

[Route("projects/{projectId}/users/{userId}")]
public class SaveProjectUserRoleController(
    IDbContextFactory dbContextFactory,
    SaveProjectUserRoleRepository repo,
    SaveProjectUserRoleValidator validator,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpPatch]
    public async Task<IActionResult> SaveProjectUserRole(
        string projectId,
        string userId,
        [FromBody] SaveProjectUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = validator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        if (securityResult.Role != DataConstants.ProjectRole.Admin)
            return Unauthorized(new { error = "Only project admins can perform this action." });

        var currentRole = await repo.GetMemberRoleAsync(db, projectId, userId);
        if (currentRole is null)
            return NotFound(new { error = "That user is not a member of this project." });

        if (currentRole == DataConstants.ProjectRole.Admin &&
            request.ProjectRole != DataConstants.ProjectRole.Admin)
        {
            var adminCount = await repo.CountAdminsAsync(db, projectId);
            if (adminCount <= 1)
                return BadRequest(new { error = "Cannot demote the last admin of a project." });
        }

        await repo.SaveMemberRoleAsync(db, projectId, userId, request.ProjectRole);

        return Ok();
    }
}
