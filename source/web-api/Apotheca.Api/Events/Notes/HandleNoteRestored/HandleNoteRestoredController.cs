using Apotheca.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Events.Notes.HandleNoteRestored;

[ApiController]
[Route("events/notes")]
[Authorize(Policy = "PubSubPush")]
public class HandleNoteRestoredController(
    IDbContextFactory dbContextFactory,
    HandleNoteRestoredRepository repo,
    ILogger<HandleNoteRestoredController> logger) : ControllerBase
{
    [HttpPost("note-restored")]
    public async Task<IActionResult> Handle([FromBody] PubSubPushRequest request, CancellationToken cancellationToken)
    {
        var eventData = request.DecodeMessage<NoteRestoredEvent>();
        if (eventData is null)
            return BadRequest();

        logger.LogInformation(
            "NoteRestored event received. NoteId: {NoteId}, ProjectId: {ProjectId}, AncestorCount: {AncestorCount}",
            eventData.NoteId, eventData.ProjectId, eventData.RestoredAncestors.Count);

        if (eventData.RestoredAncestors.Count == 0)
            return NoContent();

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);
        await db.BeginTransactionAsync(cancellationToken);

        var restoredItemType = eventData.IsFolder ? "folder" : "note";
        foreach (var ancestor in eventData.RestoredAncestors)
        {
            var ancestorType = ancestor.IsFolder ? "Folder" : "Note";
            var message      = $"{ancestorType} '{ancestor.Title}' restored (parent of restored {restoredItemType} '{eventData.Title}')";
            await repo.InsertProjectActivityLogAsync(db, eventData.ProjectId, ancestor.NoteId, eventData.UserId, message);
        }

        await db.CommitAsync(cancellationToken);

        return NoContent();
    }
}
