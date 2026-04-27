using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Labels.SearchLabels;

[Route("projects/{projectId}/labels")]
public class SearchLabelsController(
    IDbContextFactory dbContextFactory,
    SearchLabelsRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> SearchLabels(
        string projectId,
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<SearchLabelsResponse>());

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var labels = await repo.SearchAsync(db, projectId, q.Trim());
        return Ok(labels);
    }
}
