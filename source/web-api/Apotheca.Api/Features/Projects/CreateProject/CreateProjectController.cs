using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Projects.CreateProject;

[Route("projects")]
public class CreateProjectController(
    IDbContextFactory dbContextFactory,
    CreateProjectRepository repo,
    CreateProjectValidator validator,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var validationErrors = validator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeWorkspaceAccessAsync(db, request.WorkspaceId, requireAdmin: true, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        foreach (var member in request.Members)
        {
            if (!await repo.IsWorkspaceMemberAsync(db, request.WorkspaceId, member.UserId))
                return BadRequest(new { error = $"User '{member.UserId}' is not a member of this workspace." });
        }

        var name = request.Name.Trim();
        var summary = request.Summary?.Trim();

        await db.BeginTransactionAsync(cancellationToken);

        var projectId = await repo.CreateProjectAsync(db, request.WorkspaceId, name, summary);
        await repo.AddProjectMemberAsync(db, projectId, securityResult.UserId, DataConstants.ProjectRole.Admin);

        foreach (var member in request.Members)
            await repo.AddProjectMemberAsync(db, projectId, member.UserId, member.ProjectRole);

        await db.CommitAsync(cancellationToken);

        return Ok(new { id = projectId, name });
    }
}
