using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Labels.SearchLabels;

[Route("projects/{projectId}/labels")]
public class SearchLabelsController(
    IDbContextFactory dbContextFactory,
    SearchLabelsRepository repo) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> SearchLabels(
        string projectId,
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<SearchLabelsResponse>());

        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var hasAccess = await repo.UserHasProjectAccessAsync(db, firebaseUid, projectId);
        if (!hasAccess)
            return Forbid();

        var labels = await repo.SearchAsync(db, projectId, q.Trim());
        return Ok(labels);
    }
}
