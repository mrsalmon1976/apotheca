using Apotheca.Api.Configuration;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.SaveNoteAttachment;

[Route("projects/{projectId}/notes/{noteId}/attachments")]
public class SaveNoteAttachmentController(
    IDbContextFactory dbContextFactory,
    IAppSettings appSettings,
    StorageClient storageClient,
    SaveNoteAttachmentRepository repo,
    ISecurityProvider securityProvider,
    ILogger<SaveNoteAttachmentController> logger) : AuthenticatedBaseController
{
    [HttpPost]
    [RequestSizeLimit(10_485_760)] // 10 MB
    public async Task<IActionResult> SaveNoteAttachment(
        string projectId,
        string noteId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var noteExists = await repo.NoteExistsAsync(db, projectId, noteId);
        if (!noteExists)
            return NotFound(new { error = $"Note '{noteId}' was not found." });

        var attachmentId = NanoidDotNet.Nanoid.Generate();
        var extension    = Path.GetExtension(file.FileName);
        var fileName     = Path.GetFileName(file.FileName);
        var objectName   = $"projects/{projectId}/notes/{noteId}/{attachmentId}{extension}";

        await using var stream = file.OpenReadStream();
        await storageClient.UploadObjectAsync(
            appSettings.StorageBucketName,
            objectName,
            file.ContentType,
            stream,
            cancellationToken: cancellationToken);

        await repo.InsertNoteAttachmentAsync(db, attachmentId, projectId, noteId,
            objectName, fileName, file.ContentType, file.Length, securityResult.UserId);

        logger.LogInformation(
            "Note attachment uploaded. AttachmentId: {AttachmentId}, NoteId: {NoteId}, ProjectId: {ProjectId}, UserId: {UserId}",
            attachmentId, noteId, projectId, securityResult.UserId);

        return Ok(new
        {
            id  = attachmentId,
            url = $"/projects/{projectId}/notes/attachments/{attachmentId}",
        });
    }
}
