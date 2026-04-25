using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.CreateDocument;

[Route("projects/{projectId}/documents")]
public class CreateDocumentController(
    IDbContextFactory dbContextFactory,
    CreateDocumentRepository repo,
    ILogger<CreateDocumentController> logger) : AuthenticatedBaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateDocument(
        string projectId,
        [FromBody] CreateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var hasAccess = await repo.UserHasProjectAccessAsync(db, firebaseUid, projectId);
        if (!hasAccess)
            return Forbid();

        var userId = await repo.GetUserIdAsync(db, firebaseUid);
        if (userId is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        var id = await repo.InsertDocumentAsync(db, projectId, userId, request.ParentDocumentId);
        await repo.InsertDocumentLogAsync(db, id, userId, projectId);
        await repo.InsertProjectActivityLogAsync(db, projectId, id, userId, "Document added");

        logger.LogInformation("Document created. DocumentId: {DocumentId}, ProjectId: {ProjectId}, UserId: {UserId}", id, projectId, userId);

        return CreatedAtAction(nameof(CreateDocument), new { projectId }, new { id });
    }
}
