using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.GetDocumentLinks;

[Route("projects/{projectId}/documents")]
public class GetDocumentLinksController(
    IDbContextFactory dbContextFactory,
    GetDocumentLinksRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet("{documentId}/links")]
    public async Task<IActionResult> GetDocumentLinks(
        string projectId,
        string documentId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var links = await repo.GetLinksAsync(db, projectId, documentId);
        return Ok(links);
    }
}
