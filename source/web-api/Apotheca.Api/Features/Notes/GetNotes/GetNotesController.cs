using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.GetNotes;

[Route("projects/{projectId}/notes")]
public class GetNotesController(
    IDbContextFactory dbContextFactory,
    GetNotesRepository repo) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetNotes(
        string projectId,
        [FromQuery] string? parentId,
        CancellationToken cancellationToken)
    {
        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var hasAccess = await repo.UserHasProjectAccessAsync(db, firebaseUid, projectId);
        if (!hasAccess)
            return Forbid();

        var notes = await repo.GetNotesAsync(db, projectId, parentId);
        return Ok(notes.ToResponse());
    }
}
