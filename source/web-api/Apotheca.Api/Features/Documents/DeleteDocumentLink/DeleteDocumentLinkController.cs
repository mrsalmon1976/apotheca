using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.DeleteDocumentLink;

[Route("projects/{projectId}/documents")]
public class DeleteDocumentLinkController(
    IDbContextFactory dbContextFactory,
    DeleteDocumentLinkRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpDelete("{documentId}/links/{linkId}")]
    public async Task<IActionResult> DeleteDocumentLink(
        string projectId,
        string documentId,
        string linkId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var deleted = await repo.DeleteLinkAsync(db, projectId, documentId, linkId);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
