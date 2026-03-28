using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.SaveNote;

[Route("projects/{projectId}/notes/{noteId}")]
public class SaveNoteController(
    IDbContextFactory dbContextFactory,
    SaveNoteRepository repo,
    SaveNoteValidator validator) : AuthenticatedBaseController
{
    [HttpPatch]
    public async Task<IActionResult> SaveNote(
        string projectId,
        string noteId,
        [FromBody] SaveNoteRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = validator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

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

        var noteExists = await repo.NoteExistsAsync(db, projectId, noteId);
        if (!noteExists)
            return NotFound(new { error = $"Note '{noteId}' was not found." });

        await db.BeginTransactionAsync(cancellationToken);

        if (request.Title is not null || request.Body is not null)
            await repo.UpdateNoteCoreAsync(db, projectId, noteId, request.Title?.Trim(), request.Body);

        if (request.Labels is not null)
        {
            await repo.DeleteNoteLabelsAsync(db, noteId);
            var labelTexts = request.Labels
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var labelText in labelTexts)
            {
                var labelId = await repo.UpsertLabelAsync(db, projectId, userId, labelText);
                await repo.InsertNoteLabelAsync(db, noteId, labelId);
            }
        }

        await db.CommitAsync(cancellationToken);

        return Ok();
    }
}
