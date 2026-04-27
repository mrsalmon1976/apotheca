using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.GetDocument;

[Route("projects/{projectId}/documents")]
public class GetDocumentController(
    IDbContextFactory dbContextFactory,
    GetDocumentRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet("{documentId}")]
    public async Task<IActionResult> GetDocument(
        string projectId,
        string documentId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var document = await repo.GetDocumentAsync(db, projectId, documentId);
        if (document is null)
            return NotFound();

        return Ok(document);
    }
}
