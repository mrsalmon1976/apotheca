using Apotheca.Api.Configuration;
using Apotheca.Api.Events;
using Apotheca.Api.Events.Documents.DocumentUploaded;
using Apotheca.Api.Providers;
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
    ISecurityProvider securityProvider,
    IEventPublisher eventPublisher,
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
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var fileName      = Path.GetFileName(file.FileName);
        var fileExtension = Path.GetExtension(fileName);
        var resolvedTitle = !string.IsNullOrWhiteSpace(title) ? title.Trim() : Path.GetFileNameWithoutExtension(fileName);

        var documentId = NanoidDotNet.Nanoid.Generate();
        var objectName = $"projects/{projectId}/documents/{documentId}/{fileName}";

        await using var stream = file.OpenReadStream();
        await storageClient.UploadObjectAsync(
            appSettings.StorageBucketName,
            objectName,
            file.ContentType,
            stream,
            cancellationToken: cancellationToken);

        var insertedId = await repo.InsertDocumentWithIdAsync(db, documentId, projectId, securityResult.UserId, parentId,
            resolvedTitle, fileName, fileExtension, file.ContentType, file.Length, objectName);

        await repo.InsertDocumentLogAsync(db, insertedId, securityResult.UserId, projectId);
        await repo.InsertProjectActivityLogAsync(db, projectId, insertedId, securityResult.UserId, "Document uploaded");
        await repo.UpsertSearchAsync(db, projectId, insertedId, resolvedTitle);

        await eventPublisher.PublishAsync(DocumentUploadedEvent.TopicId, new DocumentUploadedEvent
        {
            DocumentId    = insertedId,
            ProjectId     = projectId,
            BlobReference = objectName,
            FileExtension = fileExtension,
        }, cancellationToken);

        logger.LogInformation(
            "Document uploaded. DocumentId: {DocumentId}, ProjectId: {ProjectId}, UserId: {UserId}, ObjectName: {ObjectName}",
            insertedId, projectId, securityResult.UserId, objectName);

        return CreatedAtAction(nameof(UploadDocument), new { projectId }, new { id = insertedId });
    }
}
