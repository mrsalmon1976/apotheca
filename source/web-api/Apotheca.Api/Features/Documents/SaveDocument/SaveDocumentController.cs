using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.SaveDocument;

[Route("projects/{projectId}/documents/{documentId}")]
public class SaveDocumentController(
    IDbContextFactory dbContextFactory,
    SaveDocumentRepository repo,
    SaveDocumentValidator validator,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
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

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var documentExists = await repo.DocumentExistsAsync(db, projectId, documentId);
        if (!documentExists)
            return NotFound(new { error = $"Document '{documentId}' was not found." });

        await db.BeginTransactionAsync(cancellationToken);

        if (request.Title is not null)
        {
            await repo.UpdateDocumentTitleAsync(db, projectId, documentId, request.Title.Trim());
            await repo.UpsertSearchAsync(db, projectId, documentId, request.Title.Trim());
        }

        if (request.Labels is not null)
        {
            await repo.DeleteDocumentLabelsAsync(db, documentId);
            var labelTexts = request.Labels
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var labelText in labelTexts)
            {
                var labelId = await repo.UpsertLabelAsync(db, projectId, securityResult.UserId, labelText);
                await repo.InsertDocumentLabelAsync(db, documentId, labelId);
            }
        }

        await db.CommitAsync(cancellationToken);

        return Ok();
    }
}
