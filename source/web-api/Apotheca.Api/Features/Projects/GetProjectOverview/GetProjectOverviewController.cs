using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Projects.GetProjectOverview;

[Route("projects/{projectId}/overview")]
public class GetProjectOverviewController(
    IDbContextFactory dbContextFactory,
    GetProjectOverviewRepository repo) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetProjectOverview(
        string projectId,
        CancellationToken cancellationToken)
    {
        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var hasAccess = await repo.UserHasProjectAccessAsync(db, firebaseUid, projectId);
        if (!hasAccess)
            return Forbid();

        var openTaskCount   = await repo.GetOpenTaskCountAsync(db, firebaseUid, projectId);
        var noteCount       = await repo.GetNoteCountAsync(db, projectId);
        var documentCount   = await repo.GetDocumentCountAsync(db, projectId);

        return Ok(new GetProjectOverviewResponse
        {
            OpenTaskCount = openTaskCount,
            NoteCount     = noteCount,
            DocumentCount = documentCount,
        });
    }
}
