using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.MoveDocument;

[Route("projects/{projectId}/documents/{documentId}/move")]
public class MoveDocumentController(
    IDbContextFactory dbContextFactory,
    MoveDocumentRepository repo,
    ISecurityProvider securityProvider,
    ILogger<MoveDocumentController> logger) : AuthenticatedBaseController
{
    [HttpPatch]
    public async Task<IActionResult> MoveDocument(
        string projectId,
        string documentId,
        [FromBody] MoveDocumentRequest request,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var document = await repo.GetDocumentInfoAsync(db, projectId, documentId);
        if (document is null)
            return NotFound(new { error = $"Document '{documentId}' was not found." });

        var targetFolderId = string.IsNullOrWhiteSpace(request.TargetFolderId) ? null : request.TargetFolderId;

        if (targetFolderId == documentId)
            return BadRequest(new { error = "An item cannot be moved into itself." });

        if (targetFolderId == document.ParentDocumentId)
            return Ok();

        if (targetFolderId is not null)
        {
            var targetExists = await repo.TargetFolderExistsAsync(db, projectId, targetFolderId);
            if (!targetExists)
                return NotFound(new { error = $"Target folder '{targetFolderId}' was not found." });
        }

        if (document.IsFolder && targetFolderId is not null)
        {
            var wouldCreateCycle = await repo.WouldCreateCycleAsync(db, documentId, targetFolderId);
            if (wouldCreateCycle)
                return BadRequest(new { error = "A folder cannot be moved into itself or one of its subfolders." });
        }

        var targetTitle = targetFolderId is not null ? await repo.GetFolderTitleAsync(db, targetFolderId) : null;
        var itemLabel = document.IsFolder ? "Folder" : "Document";
        var logMessage = targetTitle is not null
            ? $"{itemLabel} '{document.Title}' moved to '{targetTitle}'"
            : $"{itemLabel} '{document.Title}' moved to root";

        await db.BeginTransactionAsync(cancellationToken);

        await repo.MoveDocumentAsync(db, projectId, documentId, targetFolderId);
        await repo.InsertProjectActivityLogAsync(db, projectId, documentId, securityResult.UserId, logMessage);

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Document moved. DocumentId: {DocumentId}, ProjectId: {ProjectId}, TargetFolderId: {TargetFolderId}, UserId: {UserId}", documentId, projectId, targetFolderId, securityResult.UserId);

        return Ok();
    }
}
