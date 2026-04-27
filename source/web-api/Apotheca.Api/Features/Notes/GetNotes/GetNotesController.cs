using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.GetNotes;

[Route("projects/{projectId}/notes")]
public class GetNotesController(
    IDbContextFactory dbContextFactory,
    GetNotesRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetNotes(
        string projectId,
        [FromQuery] string? parentId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var notes = await repo.GetNotesAsync(db, projectId, parentId);
        return Ok(notes);
    }
}
