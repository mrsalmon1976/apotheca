using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.MoveNote;

[Route("projects/{projectId}/notes/{noteId}/move")]
public class MoveNoteController(
    IDbContextFactory dbContextFactory,
    MoveNoteRepository repo,
    ISecurityProvider securityProvider,
    ILogger<MoveNoteController> logger) : AuthenticatedBaseController
{
    [HttpPatch]
    public async Task<IActionResult> MoveNote(
        string projectId,
        string noteId,
        [FromBody] MoveNoteRequest request,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var note = await repo.GetNoteInfoAsync(db, projectId, noteId);
        if (note is null)
            return NotFound(new { error = $"Note '{noteId}' was not found." });

        var targetFolderId = string.IsNullOrWhiteSpace(request.TargetFolderId) ? null : request.TargetFolderId;

        if (targetFolderId == noteId)
            return BadRequest(new { error = "An item cannot be moved into itself." });

        if (targetFolderId == note.ParentNoteId)
            return Ok();

        if (targetFolderId is not null)
        {
            var targetExists = await repo.TargetFolderExistsAsync(db, projectId, targetFolderId);
            if (!targetExists)
                return NotFound(new { error = $"Target folder '{targetFolderId}' was not found." });
        }

        if (note.IsFolder && targetFolderId is not null)
        {
            var wouldCreateCycle = await repo.WouldCreateCycleAsync(db, noteId, targetFolderId);
            if (wouldCreateCycle)
                return BadRequest(new { error = "A folder cannot be moved into itself or one of its subfolders." });
        }

        var targetTitle = targetFolderId is not null ? await repo.GetFolderTitleAsync(db, targetFolderId) : null;
        var itemLabel = note.IsFolder ? "Folder" : "Note";
        var logMessage = targetTitle is not null
            ? $"{itemLabel} '{note.Title}' moved to '{targetTitle}'"
            : $"{itemLabel} '{note.Title}' moved to root";

        await db.BeginTransactionAsync(cancellationToken);

        await repo.MoveNoteAsync(db, projectId, noteId, targetFolderId);
        await repo.InsertProjectActivityLogAsync(db, projectId, noteId, securityResult.UserId, logMessage);

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Note moved. NoteId: {NoteId}, ProjectId: {ProjectId}, TargetFolderId: {TargetFolderId}, UserId: {UserId}", noteId, projectId, targetFolderId, securityResult.UserId);

        return Ok();
    }
}
