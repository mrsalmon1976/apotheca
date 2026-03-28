using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.CreateNote;

[Route("projects/{projectId}/notes")]
public class CreateNoteController(
    IDbContextFactory dbContextFactory,
    CreateNoteRepository repo) : AuthenticatedBaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateNote(
        string projectId,
        [FromBody] CreateNoteRequest request,
        CancellationToken cancellationToken)
    {
        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var hasAccess = await repo.UserHasProjectAccessAsync(db, firebaseUid, projectId);
        if (!hasAccess)
            return Forbid();

        var userId = await repo.GetUserIdAsync(db, firebaseUid);
        if (userId is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        var id = await repo.InsertNoteAsync(db, projectId, userId, request.ParentNoteId);

        return CreatedAtAction(nameof(CreateNote), new { projectId }, new { id });
    }
}
