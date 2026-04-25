using Apotheca.Api.Events;
using Apotheca.Api.Events.Documents;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.RestoreDocument;

[Route("projects/{projectId}/documents")]
public class RestoreDocumentController(
    IDbContextFactory dbContextFactory,
    RestoreDocumentRepository repo,
    IEventPublisher eventPublisher,
    ILogger<RestoreDocumentController> logger) : AuthenticatedBaseController
{
    [HttpPost("{documentId}/restore")]
    public async Task<IActionResult> RestoreDocument(
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

        var document = await repo.GetDeletedDocumentInfoAsync(db, projectId, documentId);
        if (document is null)
            return NotFound();

        await db.BeginTransactionAsync(cancellationToken);

        await repo.RestoreDocumentAsync(db, documentId);
        var restoredAncestors = await repo.RestoreAncestorsAsync(db, documentId);

        await repo.InsertDocumentLogAsync(db, documentId, userId, projectId, document.Title, document.IsFolder);

        var logMessage = document.IsFolder
            ? $"Folder '{document.Title}' restored"
            : $"Document '{document.Title}' restored";
        await repo.InsertProjectActivityLogAsync(db, projectId, documentId, userId, logMessage);

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Document restored. DocumentId: {DocumentId}, ProjectId: {ProjectId}, UserId: {UserId}, AncestorsRestored: {AncestorCount}", documentId, projectId, userId, restoredAncestors.Count);

        await eventPublisher.PublishAsync(DocumentRestoredEvent.TopicId, new DocumentRestoredEvent
        {
            DocumentId        = documentId,
            ProjectId         = projectId,
            UserId            = userId,
            Title             = document.Title,
            IsFolder          = document.IsFolder,
            RestoredAncestors = restoredAncestors,
        }, cancellationToken);

        return NoContent();
    }
}
