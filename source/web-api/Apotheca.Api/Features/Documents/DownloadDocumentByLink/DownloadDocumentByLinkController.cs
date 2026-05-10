using Apotheca.Api.Configuration;
using Apotheca.Data;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.DownloadDocumentByLink;

[Route("documents/public")]
public class DownloadDocumentByLinkController(
    IDbContextFactory dbContextFactory,
    IAppSettings appSettings,
    StorageClient storageClient,
    DownloadDocumentByLinkRepository repo) : ControllerBase
{
    [HttpGet("{linkId}")]
    public async Task<IActionResult> DownloadDocumentByLink(
        string linkId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var info = await repo.GetDownloadInfoAsync(db, linkId);
        if (info is null)
            return NotFound();

        var (blobReference, fileName, mimetype) = info.Value;

        var stream = new MemoryStream();
        await storageClient.DownloadObjectAsync(
            appSettings.StorageBucketName,
            blobReference,
            stream,
            cancellationToken: cancellationToken);
        stream.Position = 0;

        return File(stream, mimetype, fileName);
    }
}
