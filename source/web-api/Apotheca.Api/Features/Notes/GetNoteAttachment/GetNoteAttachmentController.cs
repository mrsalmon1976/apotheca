using Apotheca.Api.Configuration;
using Apotheca.Data;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.GetNoteAttachment;

[ApiController]
[Route("projects/{projectId}/notes/attachments")]
public class GetNoteAttachmentController(
    IDbContextFactory dbContextFactory,
    IAppSettings appSettings,
    StorageClient storageClient,
    GetNoteAttachmentRepository repo) : ControllerBase
{
    [HttpGet("{attachmentId}")]
    public async Task<IActionResult> GetNoteAttachment(
        string projectId,
        string attachmentId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var attachment = await repo.GetAttachmentAsync(db, projectId, attachmentId);
        if (attachment is null)
            return NotFound();

        var gcsObject = new Google.Apis.Storage.v1.Data.Object { Bucket = appSettings.StorageBucketName, Name = attachment.BlobReference };
        var memStream = new MemoryStream();
        await storageClient.DownloadObjectAsync(gcsObject, memStream, cancellationToken: cancellationToken);

        memStream.Position = 0;
        return File(memStream, attachment.Mimetype, attachment.FileName);
    }
}
