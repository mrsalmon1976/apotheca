using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.GetDocument;

[Route("projects/{projectId}/documents")]
public class GetDocumentController(
    IDbContextFactory dbContextFactory,
    GetDocumentRepository repo) : AuthenticatedBaseController
{
    [HttpGet("{documentId}")]
    public async Task<IActionResult> GetDocument(
        string projectId,
        string documentId,
        CancellationToken cancellationToken)
    {
        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var hasAccess = await repo.UserHasProjectAccessAsync(db, firebaseUid, projectId);
        if (!hasAccess)
            return Forbid();

        var document = await repo.GetDocumentAsync(db, projectId, documentId);
        if (document is null)
            return NotFound();

        return Ok(document);
    }
}
