using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Workspaces.AddWorkspaceUser;

[Route("workspaces/{workspaceId}/users")]
public class AddWorkspaceUserController(
    IDbContextFactory dbContextFactory,
    AddWorkspaceUserRepository repo,
    AddWorkspaceUserValidator validator,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpPost]
    public async Task<IActionResult> AddWorkspaceUser(
        string workspaceId,
        [FromBody] AddWorkspaceUserRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = validator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeWorkspaceAccessAsync(db, workspaceId, requireAdmin: true, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var email = request.Email.Trim();
        var userId = await repo.GetUserIdByEmailAsync(db, email);
        if (userId is null)
            return BadRequest(new { error = $"No Apotheca account found for '{email}'. They'll need to sign up first." });

        if (await repo.IsMemberAsync(db, workspaceId, userId))
            return Conflict(new { error = $"'{email}' is already a member of this workspace." });

        await repo.AddMemberAsync(db, workspaceId, userId, request.WorkspaceRole);

        return Ok();
    }
}
