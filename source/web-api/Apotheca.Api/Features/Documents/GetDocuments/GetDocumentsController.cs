using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.GetDocuments;

[Route("projects/{projectId}/documents")]
public class GetDocumentsController(
    IDbContextFactory dbContextFactory,
    GetDocumentsRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetDocuments(
        string projectId,
        [FromQuery] string? parentId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var documents = await repo.GetDocumentsAsync(db, projectId, parentId);
        return Ok(documents);
    }
}
