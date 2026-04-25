using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.GetDocuments;

[Route("projects/{projectId}/documents")]
public class GetDocumentsController(
    IDbContextFactory dbContextFactory,
    GetDocumentsRepository repo) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetDocuments(
        string projectId,
        [FromQuery] string? parentId,
        CancellationToken cancellationToken)
    {
        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var hasAccess = await repo.UserHasProjectAccessAsync(db, firebaseUid, projectId);
        if (!hasAccess)
            return Forbid();

        var documents = await repo.GetDocumentsAsync(db, projectId, parentId);
        return Ok(documents);
    }
}
