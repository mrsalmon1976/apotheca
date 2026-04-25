using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.SaveDocumentFolder;

[Route("projects/{projectId}/documents/folders")]
public class SaveDocumentFolderController(
    IDbContextFactory dbContextFactory,
    SaveDocumentFolderRepository repo,
    SaveDocumentFolderValidator validator,
    ILogger<SaveDocumentFolderController> logger) : AuthenticatedBaseController
{
    [HttpPost]
    public async Task<IActionResult> SaveDocumentFolder(
        string projectId,
        [FromBody] SaveDocumentFolderRequest request,
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

        var id = await repo.InsertDocumentFolderAsync(db, projectId, userId, request.Title.Trim(), request.ParentDocumentId);

        var labelTexts = request.Labels
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var labelText in labelTexts)
        {
            var labelId = await repo.UpsertLabelAsync(db, projectId, userId, labelText);
            await repo.InsertDocumentLabelAsync(db, id, labelId);
        }

        await repo.InsertDocumentLogAsync(db, id, userId, projectId);
        await repo.InsertProjectActivityLogAsync(db, projectId, id, userId, $"Document folder '{request.Title.Trim()}' added");

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Document folder created. FolderId: {FolderId}, ProjectId: {ProjectId}, UserId: {UserId}", id, projectId, userId);

        return CreatedAtAction(nameof(SaveDocumentFolder), new { projectId }, new { id });
    }
}
