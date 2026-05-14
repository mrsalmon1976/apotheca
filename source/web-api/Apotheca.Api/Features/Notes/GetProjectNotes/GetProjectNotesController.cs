using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.GetProjectNotes;

[Route("projects/{projectId}/notes")]
public class GetProjectNotesController(
    IDbContextFactory dbContextFactory,
    GetProjectNotesRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentNotes(
        string projectId,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var notes = await repo.GetRecentNotesAsync(db, projectId, limit);
        return Ok(notes.ToResponse());
    }
}
