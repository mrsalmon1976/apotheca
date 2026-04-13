using Apotheca.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Events.Notes.HandleNoteDeleted;

[ApiController]
[Route("events/notes")]
[Authorize(Policy = "PubSubPush")]
public class HandleNoteDeletedController(
    IDbContextFactory dbContextFactory,
    HandleNoteDeletedRepository repo,
    ILogger<HandleNoteDeletedController> logger) : ControllerBase
{
    [HttpPost("note-deleted")]
    public async Task<IActionResult> Handle([FromBody] PubSubPushRequest request, CancellationToken cancellationToken)
    {
        var eventData = request.DecodeMessage<NoteDeletedEvent>();
        if (eventData is null)
            return BadRequest();

        logger.LogInformation(
            "NoteDeleted event received. NoteId: {NoteId}, ProjectId: {ProjectId}, IsFolder: {IsFolder}",
            eventData.NoteId, eventData.ProjectId, eventData.IsFolder);

        if (!eventData.IsFolder)
            return NoContent();

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);
        await db.BeginTransactionAsync(cancellationToken);

        var descendants = await repo.SoftDeleteDescendantsAsync(db, eventData.NoteId);

        foreach (var item in descendants)
        {
            var itemType = item.IsFolder ? "Folder" : "Note";
            var message  = $"{itemType} '{item.Title}' deleted (child of deleted folder '{eventData.Title}')";
            await repo.InsertProjectActivityLogAsync(db, eventData.ProjectId, item.Id, eventData.UserId, message);
        }

        await db.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Cascade delete complete. NoteId: {NoteId}, DescendantCount: {DescendantCount}",
            eventData.NoteId, descendants.Count);

        return NoContent();
    }
}
