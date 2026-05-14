using Apotheca.Api.Events;
using Apotheca.Api.Events.Documents.DocumentRestored;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.RestoreDocument;

[Route("projects/{projectId}/documents")]
public class RestoreDocumentController(
    IDbContextFactory dbContextFactory,
    RestoreDocumentRepository repo,
    ISecurityProvider securityProvider,
    IEventPublisher eventPublisher,
    ILogger<RestoreDocumentController> logger) : AuthenticatedBaseController
{
    [HttpPost("{documentId}/restore")]
    public async Task<IActionResult> RestoreDocument(
        string projectId,
        string documentId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var document = await repo.GetDeletedDocumentInfoAsync(db, projectId, documentId);
        if (document is null)
            return NotFound();

        await db.BeginTransactionAsync(cancellationToken);

        await repo.RestoreDocumentAsync(db, documentId);
        var restoredAncestors = await repo.RestoreAncestorsAsync(db, documentId);

        await repo.InsertDocumentLogAsync(db, documentId, securityResult.UserId, projectId, document.Title, document.IsFolder);

        var logMessage = document.IsFolder
            ? $"Folder '{document.Title}' restored"
            : $"Document '{document.Title}' restored";
        await repo.InsertProjectActivityLogAsync(db, projectId, documentId, securityResult.UserId, logMessage);

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Document restored. DocumentId: {DocumentId}, ProjectId: {ProjectId}, UserId: {UserId}, AncestorsRestored: {AncestorCount}",
            documentId, projectId, securityResult.UserId, restoredAncestors.Count);

        await eventPublisher.PublishAsync(DocumentRestoredEvent.TopicId, new DocumentRestoredEvent
        {
            DocumentId        = documentId,
            ProjectId         = projectId,
            UserId            = securityResult.UserId,
            Title             = document.Title,
            IsFolder          = document.IsFolder,
            RestoredAncestors = restoredAncestors,
        }, cancellationToken);

        return NoContent();
    }
}
