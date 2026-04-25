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

        var notes     = await repo.GetDeletedNotesAsync(db, projectId);
        var documents = await repo.GetDeletedDocumentsAsync(db, projectId);
        var entries   = notes.Concat(documents).OrderByDescending(e => e.DeletedAt);
        return Ok(entries);
    }
}
