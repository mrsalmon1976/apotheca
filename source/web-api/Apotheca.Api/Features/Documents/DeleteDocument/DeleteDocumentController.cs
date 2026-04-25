using Apotheca.Api.Events;
using Apotheca.Api.Events.Documents;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.DeleteDocument;

[Route("projects/{projectId}/documents")]
public class DeleteDocumentController(
    IDbContextFactory dbContextFactory,
    DeleteDocumentRepository repo,
    IEventPublisher eventPublisher,
    ILogger<DeleteDocumentController> logger) : AuthenticatedBaseController
{
    [HttpDelete("{documentId}")]
    public async Task<IActionResult> DeleteDocument(
        string projectId,
        string documentId,
        CancellationToken cancellationToken)
    {
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

        var document = await repo.GetDocumentInfoAsync(db, projectId, documentId);
        if (document is null)
            return NotFound();

        await db.BeginTransactionAsync(cancellationToken);

        await repo.SoftDeleteDocumentAsync(db, documentId);

        await repo.InsertDocumentLogAsync(db, documentId, userId, projectId, document.Title, document.IsFolder);

        var logMessage = document.IsFolder
            ? $"Folder '{document.Title}' deleted"
            : $"Document '{document.Title}' deleted";
        await repo.InsertProjectActivityLogAsync(db, projectId, documentId, userId, logMessage);

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Document deleted. DocumentId: {DocumentId}, ProjectId: {ProjectId}, UserId: {UserId}", documentId, projectId, userId);

        await eventPublisher.PublishAsync(DocumentDeletedEvent.TopicId, new DocumentDeletedEvent
        {
            DocumentId = documentId,
            ProjectId  = projectId,
            UserId     = userId,
            Title      = document.Title,
            IsFolder   = document.IsFolder,
            DeletedAt  = DateTimeOffset.UtcNow,
        }, cancellationToken);

        return NoContent();
    }
}
