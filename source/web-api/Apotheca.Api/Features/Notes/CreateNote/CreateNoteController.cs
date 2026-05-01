using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.CreateNote;

[Route("projects/{projectId}/notes")]
public class CreateNoteController(
    IDbContextFactory dbContextFactory,
    CreateNoteRepository repo,
    ISecurityProvider securityProvider,
    ILogger<CreateNoteController> logger) : AuthenticatedBaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateNote(
        string projectId,
        [FromBody] CreateNoteRequest request,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var id = await repo.InsertNoteAsync(db, projectId, securityResult.UserId, request.ParentNoteId);
        await repo.InsertNoteLogAsync(db, id, securityResult.UserId, projectId);
        await repo.InsertProjectActivityLogAsync(db, projectId, id, securityResult.UserId, "Note added");
        await repo.UpsertSearchAsync(db, projectId, id, "New Note", "");

        logger.LogInformation("Note created. NoteId: {NoteId}, ProjectId: {ProjectId}, UserId: {UserId}", id, projectId, securityResult.UserId);

        return CreatedAtAction(nameof(CreateNote), new { projectId }, new { id });
    }
}
