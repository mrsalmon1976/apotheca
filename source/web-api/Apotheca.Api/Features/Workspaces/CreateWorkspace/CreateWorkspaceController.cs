using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Workspaces.CreateWorkspace;

[Route("workspaces")]
public class CreateWorkspaceController(
    IDbContextFactory dbContextFactory,
    CreateWorkspaceRepository repo,
    CreateWorkspaceValidator validator,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var validationErrors = validator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeAccessAsync(db, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var name = request.Name.Trim();

        await db.BeginTransactionAsync(cancellationToken);

        var workspaceId = await repo.CreateWorkspaceAsync(db, name);
        await repo.CreateWorkspaceMemberAsync(db, workspaceId, securityResult.UserId, DataConstants.WorkspaceRole.Admin);
        await repo.SetCurrentWorkspaceAsync(db, securityResult.UserId, workspaceId);

        await db.CommitAsync(cancellationToken);

        return Ok(new { id = workspaceId, name });
    }
}
