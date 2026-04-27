using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.GetNote;

[Route("projects/{projectId}/notes")]
public class GetNoteController(
    IDbContextFactory dbContextFactory,
    GetNoteRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet("{noteId}")]
    public async Task<IActionResult> GetNote(
        string projectId,
        string noteId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var note = await repo.GetNoteAsync(db, projectId, noteId);
        if (note is null)
            return NotFound();

        return Ok(note);
    }
}
