using Apotheca.Api.Events;
using Apotheca.Api.Events.Documents.DocumentDeleted;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.DeleteDocument;

[Route("projects/{projectId}/documents")]
public class DeleteDocumentController(
    IDbContextFactory dbContextFactory,
    DeleteDocumentRepository repo,
    ISecurityProvider securityProvider,
    IEventPublisher eventPublisher,
    ILogger<DeleteDocumentController> logger) : AuthenticatedBaseController
{
    [HttpDelete("{documentId}")]
    public async Task<IActionResult> DeleteDocument(
        string projectId,
        string documentId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var document = await repo.GetDocumentInfoAsync(db, projectId, documentId);
        if (document is null)
            return NotFound();

        await db.BeginTransactionAsync(cancellationToken);

        await repo.SoftDeleteDocumentAsync(db, documentId);

        await repo.InsertDocumentLogAsync(db, documentId, securityResult.UserId, projectId, document.Title, document.IsFolder);

        var logMessage = document.IsFolder
            ? $"Folder '{document.Title}' deleted"
            : $"Document '{document.Title}' deleted";
        await repo.InsertProjectActivityLogAsync(db, projectId, documentId, securityResult.UserId, logMessage);

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Document deleted. DocumentId: {DocumentId}, ProjectId: {ProjectId}, UserId: {UserId}", documentId, projectId, securityResult.UserId);

        await eventPublisher.PublishAsync(DocumentDeletedEvent.TopicId, new DocumentDeletedEvent
        {
            DocumentId = documentId,
            ProjectId  = projectId,
            UserId     = securityResult.UserId,
            Title      = document.Title,
            IsFolder   = document.IsFolder,
            DeletedAt  = DateTimeOffset.UtcNow,
        }, cancellationToken);

        return NoContent();
    }
}
