using Apotheca.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Events.Documents.HandleDocumentRestored;

[ApiController]
[Route("events/documents")]
[Authorize(Policy = "PubSubPush")]
public class HandleDocumentRestoredController(
    IDbContextFactory dbContextFactory,
    HandleDocumentRestoredRepository repo,
    ILogger<HandleDocumentRestoredController> logger) : ControllerBase
{
    [HttpPost("document-restored")]
    public async Task<IActionResult> Handle([FromBody] PubSubPushRequest request, CancellationToken cancellationToken)
    {
        var eventData = request.DecodeMessage<DocumentRestoredEvent>();
        if (eventData is null)
            return BadRequest();

        logger.LogInformation(
            "DocumentRestored event received. DocumentId: {DocumentId}, ProjectId: {ProjectId}, AncestorCount: {AncestorCount}",
            eventData.DocumentId, eventData.ProjectId, eventData.RestoredAncestors.Count);

        if (eventData.RestoredAncestors.Count == 0)
            return NoContent();

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);
        await db.BeginTransactionAsync(cancellationToken);

        var restoredItemType = eventData.IsFolder ? "folder" : "document";
        foreach (var ancestor in eventData.RestoredAncestors)
        {
            var ancestorType = ancestor.IsFolder ? "Folder" : "Document";
            var message      = $"{ancestorType} '{ancestor.Title}' restored (parent of restored {restoredItemType} '{eventData.Title}')";
            await repo.InsertProjectActivityLogAsync(db, eventData.ProjectId, ancestor.DocumentId, eventData.UserId, message);
        }

        await db.CommitAsync(cancellationToken);

        return NoContent();
    }
}
