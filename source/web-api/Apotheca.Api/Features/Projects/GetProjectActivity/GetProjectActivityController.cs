using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Projects.GetProjectActivity;

[Route("projects/{projectId}/activity")]
public class GetProjectActivityController(
    IDbContextFactory dbContextFactory,
    GetProjectActivityRepository repo) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetProjectActivity(
        string projectId,
        CancellationToken cancellationToken)
    {
        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var hasAccess = await repo.UserHasProjectAccessAsync(db, firebaseUid, projectId);
        if (!hasAccess)
            return Forbid();

        var entries = await repo.GetProjectActivityAsync(db, projectId);
        return Ok(entries.ToResponse());
    }
}
