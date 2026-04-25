using Apotheca.Api.Configuration;
using Apotheca.Data;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.UploadDocument;

[Route("projects/{projectId}/documents")]
public class UploadDocumentController(
    IDbContextFactory dbContextFactory,
    IAppSettings appSettings,
    StorageClient storageClient,
    UploadDocumentRepository repo,
    ILogger<UploadDocumentController> logger) : AuthenticatedBaseController
{
    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> UploadDocument(
        string projectId,
        [FromQuery] string? parentId,
        IFormFile file,
        [FromForm] string? title,
        CancellationToken cancellationToken)
    {
        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var hasAccess = await repo.UserHasProjectAccessAsync(db, firebaseUid, projectId);
        if (!hasAccess)
            return Forbid();

        var userId = await repo.GetUserIdAsync(db, firebaseUid);
        if (userId is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        var fileName      = Path.GetFileName(file.FileName);
        var fileExtension = Path.GetExtension(fileName);
        var resolvedTitle = !string.IsNullOrWhiteSpace(title) ? title.Trim() : Path.GetFileNameWithoutExtension(fileName);

        // Generate a provisional ID so the object name is known before the DB insert.
        // The repository will use its own Nanoid call, so we pass the object name prefix
        // and let the repo return the final ID.
        // Simpler: upload after DB insert using the returned ID.
        var documentId = NanoidDotNet.Nanoid.Generate();
        var objectName = $"{projectId}/{documentId}/{fileName}";

        await using var stream = file.OpenReadStream();
        await storageClient.UploadObjectAsync(
            appSettings.StorageBucketName,
            objectName,
            file.ContentType,
            stream,
            cancellationToken: cancellationToken);

        var insertedId = await repo.InsertDocumentWithIdAsync(db, documentId, projectId, userId, parentId,
            resolvedTitle, fileName, fileExtension, file.ContentType, file.Length, objectName);

        await repo.InsertDocumentLogAsync(db, insertedId, userId, projectId);
        await repo.InsertProjectActivityLogAsync(db, projectId, insertedId, userId, "Document uploaded");

        logger.LogInformation(
            "Document uploaded. DocumentId: {DocumentId}, ProjectId: {ProjectId}, UserId: {UserId}, ObjectName: {ObjectName}",
            insertedId, projectId, userId, objectName);

        return CreatedAtAction(nameof(UploadDocument), new { projectId }, new { id = insertedId });
    }
}
