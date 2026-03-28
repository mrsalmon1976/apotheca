using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.SaveNoteFolder;

[Route("projects/{projectId}/notes/folders")]
public class SaveNoteFolderController(
    IDbContextFactory dbContextFactory,
    SaveNoteFolderRepository repo,
    SaveNoteFolderValidator validator) : AuthenticatedBaseController
{
    [HttpPost]
    public async Task<IActionResult> SaveNoteFolder(
        string projectId,
        [FromBody] SaveNoteFolderRequest request,
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

        await db.BeginTransactionAsync(cancellationToken);

        var id = await repo.InsertNoteFolderAsync(db, projectId, userId, request.Title.Trim(), request.ParentNoteId);

        var labelTexts = request.Labels
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var labelText in labelTexts)
        {
            var labelId = await repo.UpsertLabelAsync(db, projectId, userId, labelText);
            await repo.InsertNoteLabelAsync(db, id, labelId);
        }

        await db.CommitAsync(cancellationToken);

        return CreatedAtAction(nameof(SaveNoteFolder), new { projectId }, new { id });
    }
}
