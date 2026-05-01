using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Search;

[Route("search")]
public class SearchController(
    IDbContextFactory dbContextFactory,
    SearchRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] string types  = "note,task",
        [FromQuery] string fields = "title,body",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Ok(Array.Empty<SearchResult>());

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeAccessAsync(db, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var typeList = types
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .ToArray();

        if (typeList.Length == 0)
            typeList = ["note", "task"];

        var fieldSet = fields
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(f => f.ToLowerInvariant())
            .ToHashSet();

        var results = await repo.SearchAsync(
            db,
            securityResult.UserId,
            q.Trim(),
            typeList,
            searchTitle: fieldSet.Contains("title"),
            searchBody:  fieldSet.Contains("body") || fieldSet.Count == 0);

        return Ok(results);
    }
}
