using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Workspaces.SaveWorkspace;

[Route("workspaces/{workspaceId}")]
public class SaveWorkspaceController(
    IDbContextFactory dbContextFactory,
    SaveWorkspaceRepository repo,
    SaveWorkspaceValidator validator,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpPatch]
    public async Task<IActionResult> SaveWorkspace(
        string workspaceId,
        [FromBody] SaveWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = validator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeWorkspaceAccessAsync(db, workspaceId, requireAdmin: true, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var found = await repo.SaveWorkspaceAsync(db, workspaceId, request.Name.Trim());
        if (!found)
            return NotFound(new { error = $"Workspace '{workspaceId}' was not found." });

        return Ok();
    }
}
