using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.GetNote;

[Route("projects/{projectId}/notes")]
public class GetNoteController(
    IDbContextFactory dbContextFactory,
    GetNoteRepository repo) : AuthenticatedBaseController
{
    [HttpGet("{noteId}")]
    public async Task<IActionResult> GetNote(
        string projectId,
        string noteId,
        CancellationToken cancellationToken)
    {
        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var hasAccess = await repo.UserHasProjectAccessAsync(db, firebaseUid, projectId);
        if (!hasAccess)
            return Forbid();

        var note = await repo.GetNoteAsync(db, projectId, noteId);
        if (note is null)
            return NotFound();

        return Ok(note.ToResponse());
    }
}
