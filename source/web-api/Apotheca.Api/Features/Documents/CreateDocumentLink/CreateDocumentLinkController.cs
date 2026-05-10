using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.CreateDocumentLink;

[Route("projects/{projectId}/documents")]
public class CreateDocumentLinkController(
    IDbContextFactory dbContextFactory,
    CreateDocumentLinkRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpPost("{documentId}/links")]
    public async Task<IActionResult> CreateDocumentLink(
        string projectId,
        string documentId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var documentExists = await repo.DocumentExistsAsync(db, projectId, documentId);
        if (!documentExists)
            return NotFound();

        var link = await repo.InsertLinkAsync(db, documentId, securityResult.UserId);
        return Created(string.Empty, link);
    }
}
