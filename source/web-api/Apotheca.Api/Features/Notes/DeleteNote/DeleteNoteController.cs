using Apotheca.Api.Events;
using Apotheca.Api.Events.Notes;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.DeleteNote;

[Route("projects/{projectId}/notes")]
public class DeleteNoteController(
    IDbContextFactory dbContextFactory,
    DeleteNoteRepository repo,
    IEventPublisher eventPublisher,
    ILogger<DeleteNoteController> logger) : AuthenticatedBaseController
{
    [HttpDelete("{noteId}")]
    public async Task<IActionResult> DeleteNote(
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

        var note = await repo.GetNoteInfoAsync(db, projectId, noteId);
        if (note is null)
            return NotFound();

        await db.BeginTransactionAsync(cancellationToken);

        await repo.SoftDeleteNoteAsync(db, noteId);

        await repo.InsertNoteLogAsync(db, noteId, userId, projectId, note.Title, note.IsFolder);

        var logMessage = note.IsFolder
            ? $"Folder '{note.Title}' deleted"
            : $"Note '{note.Title}' deleted";
        await repo.InsertProjectActivityLogAsync(db, projectId, noteId, userId, logMessage);

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Note deleted. NoteId: {NoteId}, ProjectId: {ProjectId}, UserId: {UserId}", noteId, projectId, userId);

        await eventPublisher.PublishAsync(NoteDeletedEvent.TopicId, new NoteDeletedEvent
        {
            NoteId    = noteId,
            ProjectId = projectId,
            UserId    = userId,
            Title     = note.Title,
            IsFolder  = note.IsFolder,
            DeletedAt = DateTimeOffset.UtcNow,
        }, cancellationToken);

        return NoContent();
    }
}
