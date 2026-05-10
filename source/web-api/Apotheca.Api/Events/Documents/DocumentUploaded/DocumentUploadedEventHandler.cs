using Apotheca.Api.Configuration;
using Apotheca.Data;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;

namespace Apotheca.Api.Events.Documents.DocumentUploaded;

[ApiController]
[Route("events/documents")]
[Authorize(Policy = "PubSubPush")]
public class DocumentUploadedEventHandler(
    IDbContextFactory dbContextFactory,
    IAppSettings appSettings,
    StorageClient storageClient,
    DocumentUploadedEventRepository repo,
    ILogger<DocumentUploadedEventHandler> logger) : ControllerBase
{
    private static readonly HashSet<string> _textExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".txt", ".log" };

    [HttpPost("document-uploaded")]
    public async Task<IActionResult> Handle(
        [FromBody] PubSubPushRequest request,
        CancellationToken cancellationToken)
    {
        var eventData = request.DecodeMessage<DocumentUploadedEvent>();
        if (eventData is null)
            return BadRequest();

        logger.LogInformation(
            "DocumentUploaded event received. DocumentId: {DocumentId}, Extension: {Extension}",
            eventData.DocumentId, eventData.FileExtension);

        var ext    = eventData.FileExtension;
        var isPdf  = ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        var isText = _textExtensions.Contains(ext);

        if (!isText && !isPdf)
            return NoContent();

        var memStream = new MemoryStream();
        await storageClient.DownloadObjectAsync(
            appSettings.StorageBucketName,
            eventData.BlobReference,
            memStream,
            cancellationToken: cancellationToken);
        memStream.Position = 0;

        string body;
        if (isText)
        {
            using var reader = new StreamReader(memStream, System.Text.Encoding.UTF8);
            body = await reader.ReadToEndAsync(cancellationToken);
        }
        else
        {
            using var doc = PdfDocument.Open(memStream.ToArray());
            var sb = new System.Text.StringBuilder();
            foreach (var page in doc.GetPages())
                sb.AppendLine(string.Join(" ", page.GetWords().Select(w => w.Text)));
            body = sb.ToString();
        }

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);
        await repo.UpdateSearchBodyAsync(db, eventData.DocumentId, body);

        logger.LogInformation(
            "Search body updated. DocumentId: {DocumentId}, BodyLength: {BodyLength}",
            eventData.DocumentId, body.Length);

        return NoContent();
    }
}
