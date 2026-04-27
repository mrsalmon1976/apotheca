using Apotheca.Api.Events;
using Apotheca.Api.Events.Notes;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.DeleteNote;

[Route("projects/{projectId}/notes")]
public class DeleteNoteController(
    IDbContextFactory dbContextFactory,
    DeleteNoteRepository repo,
    ISecurityProvider securityProvider,
    IEventPublisher eventPublisher,
    ILogger<DeleteNoteController> logger) : AuthenticatedBaseController
{
    [HttpDelete("{noteId}")]
    public async Task<IActionResult> DeleteNote(
        string projectId,
        string noteId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var note = await repo.GetNoteInfoAsync(db, projectId, noteId);
        if (note is null)
            return NotFound();

        await db.BeginTransactionAsync(cancellationToken);

        await repo.SoftDeleteNoteAsync(db, noteId);

        await repo.InsertNoteLogAsync(db, noteId, securityResult.UserId, projectId, note.Title, note.IsFolder);

        var logMessage = note.IsFolder
            ? $"Folder '{note.Title}' deleted"
            : $"Note '{note.Title}' deleted";
        await repo.InsertProjectActivityLogAsync(db, projectId, noteId, securityResult.UserId, logMessage);

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Note deleted. NoteId: {NoteId}, ProjectId: {ProjectId}, UserId: {UserId}", noteId, projectId, securityResult.UserId);

        await eventPublisher.PublishAsync(NoteDeletedEvent.TopicId, new NoteDeletedEvent
        {
            NoteId    = noteId,
            ProjectId = projectId,
            UserId    = securityResult.UserId,
            Title     = note.Title,
            IsFolder  = note.IsFolder,
            DeletedAt = DateTimeOffset.UtcNow,
        }, cancellationToken);

        return NoContent();
    }
}
