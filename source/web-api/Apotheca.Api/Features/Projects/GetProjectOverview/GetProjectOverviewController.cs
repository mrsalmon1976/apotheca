using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Projects.GetProjectOverview;

[Route("projects/{projectId}/overview")]
public class GetProjectOverviewController(
    IDbContextFactory dbContextFactory,
    GetProjectOverviewRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetProjectOverview(
        string projectId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var openTaskCount = await repo.GetOpenTaskCountAsync(db, securityResult.FirebaseUid, projectId);
        var noteCount     = await repo.GetNoteCountAsync(db, projectId);
        var documentCount = await repo.GetDocumentCountAsync(db, projectId);

        return Ok(new GetProjectOverviewResponse
        {
            OpenTaskCount = openTaskCount,
            NoteCount     = noteCount,
            DocumentCount = documentCount,
        });
    }
}
