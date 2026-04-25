using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.SaveDocument;

[Route("projects/{projectId}/documents/{documentId}")]
public class SaveDocumentController(
    IDbContextFactory dbContextFactory,
    SaveDocumentRepository repo,
    SaveDocumentValidator validator) : AuthenticatedBaseController
{
    [HttpPatch]
    public async Task<IActionResult> SaveDocument(
        string projectId,
        string documentId,
        [FromBody] SaveDocumentRequest request,
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

        var documentExists = await repo.DocumentExistsAsync(db, projectId, documentId);
        if (!documentExists)
            return NotFound(new { error = $"Document '{documentId}' was not found." });

        await db.BeginTransactionAsync(cancellationToken);

        if (request.Title is not null)
            await repo.UpdateDocumentTitleAsync(db, projectId, documentId, request.Title.Trim());

        if (request.Labels is not null)
        {
            await repo.DeleteDocumentLabelsAsync(db, documentId);
            var labelTexts = request.Labels
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var labelText in labelTexts)
            {
                var labelId = await repo.UpsertLabelAsync(db, projectId, userId, labelText);
                await repo.InsertDocumentLabelAsync(db, documentId, labelId);
            }
        }

        await db.CommitAsync(cancellationToken);

        return Ok();
    }
}
