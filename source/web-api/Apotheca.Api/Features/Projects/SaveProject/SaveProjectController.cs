using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Projects.SaveProject;

[Route("projects/{projectId}")]
public class SaveProjectController(
    IDbContextFactory dbContextFactory,
    SaveProjectRepository repo,
    SaveProjectValidator validator,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpPatch]
    public async Task<IActionResult> SaveProject(
        string projectId,
        [FromBody] SaveProjectRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = validator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var found = await repo.SaveProjectAsync(db, projectId, request.Name.Trim(), request.Summary?.Trim());
        if (!found)
            return NotFound(new { error = $"Project '{projectId}' was not found." });

        return Ok();
    }
}
