using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.SaveDocumentFolder;

[Route("projects/{projectId}/documents/folders")]
public class SaveDocumentFolderController(
    IDbContextFactory dbContextFactory,
    SaveDocumentFolderRepository repo,
    SaveDocumentFolderValidator validator,
    ISecurityProvider securityProvider,
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

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        await db.BeginTransactionAsync(cancellationToken);

        var id = await repo.InsertDocumentFolderAsync(db, projectId, securityResult.UserId, request.Title.Trim(), request.ParentDocumentId);

        var labelTexts = request.Labels
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var labelText in labelTexts)
        {
            var labelId = await repo.UpsertLabelAsync(db, projectId, securityResult.UserId, labelText);
            await repo.InsertDocumentLabelAsync(db, id, labelId);
        }

        await repo.InsertDocumentLogAsync(db, id, securityResult.UserId, projectId);
        await repo.InsertProjectActivityLogAsync(db, projectId, id, securityResult.UserId, $"Document folder '{request.Title.Trim()}' added");

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Document folder created. FolderId: {FolderId}, ProjectId: {ProjectId}, UserId: {UserId}", id, projectId, securityResult.UserId);

        return CreatedAtAction(nameof(SaveDocumentFolder), new { projectId }, new { id });
    }
}
