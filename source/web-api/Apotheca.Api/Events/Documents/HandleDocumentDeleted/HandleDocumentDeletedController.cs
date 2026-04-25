using Apotheca.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Events.Documents.HandleDocumentDeleted;

[ApiController]
[Route("events/documents")]
[Authorize(Policy = "PubSubPush")]
public class HandleDocumentDeletedController(
    IDbContextFactory dbContextFactory,
    HandleDocumentDeletedRepository repo,
    ILogger<HandleDocumentDeletedController> logger) : ControllerBase
{
    [HttpPost("document-deleted")]
    public async Task<IActionResult> Handle([FromBody] PubSubPushRequest request, CancellationToken cancellationToken)
    {
        var eventData = request.DecodeMessage<DocumentDeletedEvent>();
        if (eventData is null)
            return BadRequest();

        logger.LogInformation(
            "DocumentDeleted event received. DocumentId: {DocumentId}, ProjectId: {ProjectId}, IsFolder: {IsFolder}",
            eventData.DocumentId, eventData.ProjectId, eventData.IsFolder);

        if (!eventData.IsFolder)
            return NoContent();

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);
        await db.BeginTransactionAsync(cancellationToken);

        var descendants = await repo.SoftDeleteDescendantsAsync(db, eventData.DocumentId);

        foreach (var item in descendants)
        {
            var itemType = item.IsFolder ? "Folder" : "Document";
            var message  = $"{itemType} '{item.Title}' deleted (child of deleted folder '{eventData.Title}')";
            await repo.InsertProjectActivityLogAsync(db, eventData.ProjectId, item.Id, eventData.UserId, message);
        }

        await db.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Cascade delete complete. DocumentId: {DocumentId}, DescendantCount: {DescendantCount}",
            eventData.DocumentId, descendants.Count);

        return NoContent();
    }
}
