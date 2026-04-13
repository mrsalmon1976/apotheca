using Apotheca.Api.Events;
using Apotheca.Api.Events.Notes;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.RestoreNote;

[Route("projects/{projectId}/notes")]
public class RestoreNoteController(
    IDbContextFactory dbContextFactory,
    RestoreNoteRepository repo,
    IEventPublisher eventPublisher,
    ILogger<RestoreNoteController> logger) : AuthenticatedBaseController
{
    [HttpPost("{noteId}/restore")]
    public async Task<IActionResult> RestoreNote(
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

        var userId = await repo.GetUserIdAsync(db, firebaseUid);
        if (userId is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        var note = await repo.GetDeletedNoteInfoAsync(db, projectId, noteId);
        if (note is null)
            return NotFound();

        await db.BeginTransactionAsync(cancellationToken);

        await repo.RestoreNoteAsync(db, noteId);
        var restoredAncestors = await repo.RestoreAncestorsAsync(db, noteId);

        await repo.InsertNoteLogAsync(db, noteId, userId, projectId, note.Title, note.IsFolder);

        var logMessage = note.IsFolder
            ? $"Folder '{note.Title}' restored"
            : $"Note '{note.Title}' restored";
        await repo.InsertProjectActivityLogAsync(db, projectId, noteId, userId, logMessage);

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Note restored. NoteId: {NoteId}, ProjectId: {ProjectId}, UserId: {UserId}, AncestorsRestored: {AncestorCount}", noteId, projectId, userId, restoredAncestors.Count);

        await eventPublisher.PublishAsync(NoteRestoredEvent.TopicId, new NoteRestoredEvent
        {
            NoteId             = noteId,
            ProjectId          = projectId,
            UserId             = userId,
            Title              = note.Title,
            IsFolder           = note.IsFolder,
            RestoredAncestors  = restoredAncestors,
        }, cancellationToken);

        return NoContent();
    }
}
