using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Projects.GetProjectRecycleBin;

[Route("projects/{projectId}/recycle-bin")]
public class GetProjectRecycleBinController(
    IDbContextFactory dbContextFactory,
    GetProjectRecycleBinRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetProjectRecycleBin(
        string projectId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var notes     = await repo.GetDeletedNotesAsync(db, projectId);
        var documents = await repo.GetDeletedDocumentsAsync(db, projectId);
        var entries   = notes.Concat(documents).OrderByDescending(e => e.DeletedAt);
        return Ok(entries);
    }
}
