using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Projects.GetProjectRecycleBin;

[Route("projects/{projectId}/recycle-bin")]
public class GetProjectRecycleBinController(
    IDbContextFactory dbContextFactory,
    GetProjectRecycleBinRepository repo) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetProjectRecycleBin(
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

        var entries = await repo.GetDeletedNotesAsync(db, projectId);
        return Ok(entries);
    }
}
