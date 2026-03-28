using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Projects.SaveProject;

[Route("projects/{projectId}")]
public class SaveProjectController(
    IDbContextFactory dbContextFactory,
    SaveProjectRepository repo,
    SaveProjectValidator validator) : AuthenticatedBaseController
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

        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var hasAccess = await repo.UserHasProjectAccessAsync(db, firebaseUid, projectId);
        if (!hasAccess)
            return Forbid();

        var found = await repo.SaveProjectAsync(db, projectId, request.Name.Trim(), request.Summary?.Trim());
        if (!found)
            return NotFound(new { error = $"Project '{projectId}' was not found." });

        return Ok();
    }
}
