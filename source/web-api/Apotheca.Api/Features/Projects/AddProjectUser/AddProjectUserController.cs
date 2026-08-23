using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Projects.AddProjectUser;

[Route("projects/{projectId}/users")]
public class AddProjectUserController(
    IDbContextFactory dbContextFactory,
    AddProjectUserRepository repo,
    AddProjectUserValidator validator,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpPost]
    public async Task<IActionResult> AddProjectUser(
        string projectId,
        [FromBody] AddProjectUserRequest request,
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

        var workspaceId = await repo.GetWorkspaceIdForProjectAsync(db, projectId);
        if (workspaceId is null)
            return NotFound(new { error = $"Project '{projectId}' was not found." });

        if (!await repo.IsWorkspaceMemberAsync(db, workspaceId, request.UserId))
            return BadRequest(new { error = "User is not a member of this project's workspace." });

        if (await repo.IsProjectMemberAsync(db, projectId, request.UserId))
            return Conflict(new { error = "User is already a member of this project." });

        await repo.AddMemberAsync(db, projectId, request.UserId, request.ProjectRole);

        return Ok();
    }
}
