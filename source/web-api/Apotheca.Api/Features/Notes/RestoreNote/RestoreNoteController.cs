using Apotheca.Api.Events;
using Apotheca.Api.Events.Notes;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.RestoreNote;

[Route("projects/{projectId}/notes")]
public class RestoreNoteController(
    IDbContextFactory dbContextFactory,
    RestoreNoteRepository repo,
    ISecurityProvider securityProvider,
    IEventPublisher eventPublisher,
    ILogger<RestoreNoteController> logger) : AuthenticatedBaseController
{
    [HttpPost("{noteId}/restore")]
    public async Task<IActionResult> RestoreNote(
        string projectId,
        string noteId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var note = await repo.GetDeletedNoteInfoAsync(db, projectId, noteId);
        if (note is null)
            return NotFound();

        await db.BeginTransactionAsync(cancellationToken);

        await repo.RestoreNoteAsync(db, noteId);
        var restoredAncestors = await repo.RestoreAncestorsAsync(db, noteId);

        await repo.InsertNoteLogAsync(db, noteId, securityResult.UserId, projectId, note.Title, note.IsFolder);

        var logMessage = note.IsFolder
            ? $"Folder '{note.Title}' restored"
            : $"Note '{note.Title}' restored";
        await repo.InsertProjectActivityLogAsync(db, projectId, noteId, securityResult.UserId, logMessage);

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Note restored. NoteId: {NoteId}, ProjectId: {ProjectId}, UserId: {UserId}, AncestorsRestored: {AncestorCount}", noteId, projectId, securityResult.UserId, restoredAncestors.Count);

        await eventPublisher.PublishAsync(NoteRestoredEvent.TopicId, new NoteRestoredEvent
        {
            NoteId             = noteId,
            ProjectId          = projectId,
            UserId             = securityResult.UserId,
            Title              = note.Title,
            IsFolder           = note.IsFolder,
            RestoredAncestors  = restoredAncestors,
        }, cancellationToken);

        return NoContent();
    }
}
