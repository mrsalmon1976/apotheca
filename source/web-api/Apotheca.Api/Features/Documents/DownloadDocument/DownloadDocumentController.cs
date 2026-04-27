using Apotheca.Api.Configuration;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Documents.DownloadDocument;

[Route("projects/{projectId}/documents")]
public class DownloadDocumentController(
    IDbContextFactory dbContextFactory,
    IAppSettings appSettings,
    StorageClient storageClient,
    DownloadDocumentRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet("{documentId}/download")]
    public async Task<IActionResult> DownloadDocument(
        string projectId,
        string documentId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var info = await repo.GetDownloadInfoAsync(db, projectId, documentId);
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
