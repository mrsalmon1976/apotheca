using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.CreateDocument;

[Route("projects/{projectId}/documents")]
public class CreateDocumentController(
    IDbContextFactory dbContextFactory,
    CreateDocumentRepository repo,
    ISecurityProvider securityProvider,
    ILogger<CreateDocumentController> logger) : AuthenticatedBaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateDocument(
        string projectId,
        [FromBody] CreateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var id = await repo.InsertDocumentAsync(db, projectId, securityResult.UserId, request.ParentDocumentId);
        await repo.InsertDocumentLogAsync(db, id, securityResult.UserId, projectId);
        await repo.InsertProjectActivityLogAsync(db, projectId, id, securityResult.UserId, "Document added");

        logger.LogInformation("Document created. DocumentId: {DocumentId}, ProjectId: {ProjectId}, UserId: {UserId}", id, projectId, securityResult.UserId);

        return CreatedAtAction(nameof(CreateDocument), new { projectId }, new { id });
    }
}
