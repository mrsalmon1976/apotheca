using System.Text;
using System.Text.Json;
using Apotheca.Api.Configuration;
using Apotheca.Api.Events;
using Apotheca.Api.Events.Documents.DocumentUploaded;
using Apotheca.Data;
using Google.Apis.Download;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Events.Documents.DocumentUploaded;

[TestFixture]
public class DocumentUploadedEventHandlerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private IAppSettings _appSettings = null!;
    private StorageClient _storageClient = null!;
    private DocumentUploadedEventRepository _repository = null!;
    private DocumentUploadedEventHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _appSettings      = Substitute.For<IAppSettings>();
        _storageClient    = Substitute.For<StorageClient>();
        _repository       = Substitute.For<DocumentUploadedEventRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _appSettings.StorageBucketName.Returns("test-bucket");

        _handler = new DocumentUploadedEventHandler(
            _dbContextFactory,
            _appSettings,
            _storageClient,
            _repository,
            Substitute.For<ILogger<DocumentUploadedEventHandler>>());
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _storageClient.Dispose();
    }

    private static PubSubPushRequest BuildRequest(DocumentUploadedEvent eventData)
    {
        var json    = JsonSerializer.Serialize(eventData);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return new PubSubPushRequest
        {
            Message      = new PubSubMessage { Data = encoded },
            Subscription = "projects/test/subscriptions/document-uploaded-sub",
        };
    }

    private void SetupDownload(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        _storageClient
            .DownloadObjectAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
                Arg.Any<DownloadObjectOptions>(), Arg.Any<CancellationToken>(),
                Arg.Any<IProgress<IDownloadProgress>>())
            .Returns(callInfo =>
            {
                callInfo.ArgAt<Stream>(2).Write(bytes, 0, bytes.Length);
                return Task.FromResult(new Google.Apis.Storage.v1.Data.Object());
            });
    }

    private void SetupPdfDownload(byte[] pdfBytes)
    {
        _storageClient
            .DownloadObjectAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
                Arg.Any<DownloadObjectOptions>(), Arg.Any<CancellationToken>(),
                Arg.Any<IProgress<IDownloadProgress>>())
            .Returns(callInfo =>
            {
                var stream = callInfo.ArgAt<Stream>(2);
                stream.Write(pdfBytes, 0, pdfBytes.Length);
                return Task.FromResult(new Google.Apis.Storage.v1.Data.Object());
            });
    }

    private static byte[] CreateMinimalPdfBytes()
    {
        const string pdf =
            "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n" +
            "2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
            "3 0 obj<</Type/Page/MediaBox[0 0 612 792]/Parent 2 0 R>>endobj\n" +
            "xref\n0 4\n0000000000 65535 f\r\n0000000009 00000 n\r\n" +
            "0000000058 00000 n\r\n0000000115 00000 n\r\n" +
            "trailer<</Size 4/Root 1 0 R>>\nstartxref\n190\n%%EOF";
        return Encoding.ASCII.GetBytes(pdf);
    }

    // --- Deserialization ---

    [Test]
    public async Task Handle_Returns400_WhenMessageDataIsEmpty()
    {
        var request = new PubSubPushRequest
        {
            Message      = new PubSubMessage { Data = string.Empty },
            Subscription = "sub",
        };

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestResult>());
    }

    // --- Unsupported extensions ---

    [TestCase(".png")]
    [TestCase(".docx")]
    [TestCase(".jpg")]
    [TestCase(".xlsx")]
    public async Task Handle_Returns204_WhenExtensionIsUnsupported(string ext)
    {
        var request = BuildRequest(new DocumentUploadedEvent
        {
            DocumentId    = "doc-1",
            ProjectId     = "proj-1",
            BlobReference = $"projects/proj-1/documents/doc-1/file{ext}",
            FileExtension = ext,
        });

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [TestCase(".png")]
    [TestCase(".docx")]
    public async Task Handle_DoesNotDownloadFromGcs_WhenExtensionIsUnsupported(string ext)
    {
        var request = BuildRequest(new DocumentUploadedEvent
        {
            DocumentId    = "doc-1",
            FileExtension = ext,
        });

        await _handler.Handle(request, CancellationToken.None);

        await _storageClient.DidNotReceive().DownloadObjectAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<DownloadObjectOptions>(), Arg.Any<CancellationToken>(),
            Arg.Any<IProgress<IDownloadProgress>>());
    }

    [TestCase(".png")]
    [TestCase(".docx")]
    public async Task Handle_DoesNotUpdateSearchBody_WhenExtensionIsUnsupported(string ext)
    {
        var request = BuildRequest(new DocumentUploadedEvent
        {
            DocumentId    = "doc-1",
            FileExtension = ext,
        });

        await _handler.Handle(request, CancellationToken.None);

        await _repository.DidNotReceive().UpdateSearchBodyAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- .txt files ---

    [Test]
    public async Task Handle_Returns204_ForTxtFile()
    {
        SetupDownload("hello world");
        var request = BuildRequest(new DocumentUploadedEvent
        {
            DocumentId    = "doc-1",
            ProjectId     = "proj-1",
            BlobReference = "projects/proj-1/documents/doc-1/notes.txt",
            FileExtension = ".txt",
        });

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Handle_UpdatesSearchBody_WithFullTextContent_ForTxtFile()
    {
        SetupDownload("hello world");
        var request = BuildRequest(new DocumentUploadedEvent
        {
            DocumentId    = "doc-1",
            ProjectId     = "proj-1",
            BlobReference = "projects/proj-1/documents/doc-1/notes.txt",
            FileExtension = ".txt",
        });

        await _handler.Handle(request, CancellationToken.None);

        await _repository.Received(1).UpdateSearchBodyAsync(_dbContext, "doc-1", "hello world");
    }

    // --- .log files ---

    [Test]
    public async Task Handle_Returns204_ForLogFile()
    {
        SetupDownload("2024-01-01 INFO Application started");
        var request = BuildRequest(new DocumentUploadedEvent
        {
            DocumentId    = "doc-1",
            FileExtension = ".log",
            BlobReference = "projects/proj-1/documents/doc-1/app.log",
        });

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Handle_UpdatesSearchBody_WithLogContent()
    {
        SetupDownload("2024-01-01 INFO Application started");
        var request = BuildRequest(new DocumentUploadedEvent
        {
            DocumentId    = "doc-1",
            FileExtension = ".log",
            BlobReference = "projects/proj-1/documents/doc-1/app.log",
        });

        await _handler.Handle(request, CancellationToken.None);

        await _repository.Received(1).UpdateSearchBodyAsync(
            _dbContext, "doc-1", "2024-01-01 INFO Application started");
    }

    // --- GCS correctness ---

    [Test]
    public async Task Handle_DownloadsFromCorrectBucketAndBlobReference()
    {
        SetupDownload("content");
        var request = BuildRequest(new DocumentUploadedEvent
        {
            DocumentId    = "doc-1",
            FileExtension = ".txt",
            BlobReference = "projects/proj-xyz/documents/doc-abc/readme.txt",
        });

        await _handler.Handle(request, CancellationToken.None);

        await _storageClient.Received(1).DownloadObjectAsync(
            "test-bucket",
            "projects/proj-xyz/documents/doc-abc/readme.txt",
            Arg.Any<Stream>(),
            Arg.Any<DownloadObjectOptions>(), Arg.Any<CancellationToken>(),
            Arg.Any<IProgress<IDownloadProgress>>());
    }

    // --- Repository correctness ---

    [Test]
    public async Task Handle_PassesCorrectDocumentId_ToRepository()
    {
        SetupDownload("some text");
        var request = BuildRequest(new DocumentUploadedEvent
        {
            DocumentId    = "doc-abc",
            FileExtension = ".txt",
            BlobReference = "projects/p/documents/doc-abc/file.txt",
        });

        await _handler.Handle(request, CancellationToken.None);

        await _repository.Received(1).UpdateSearchBodyAsync(
            _dbContext, "doc-abc", Arg.Any<string>());
    }

    // --- .pdf files ---

    [Test]
    public async Task Handle_Returns204_ForPdfFile()
    {
        SetupPdfDownload(CreateMinimalPdfBytes());
        var request = BuildRequest(new DocumentUploadedEvent
        {
            DocumentId    = "doc-1",
            FileExtension = ".pdf",
            BlobReference = "projects/proj-1/documents/doc-1/report.pdf",
        });

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Handle_DownloadsFromGcs_ForPdfFile()
    {
        SetupPdfDownload(CreateMinimalPdfBytes());
        var request = BuildRequest(new DocumentUploadedEvent
        {
            DocumentId    = "doc-1",
            FileExtension = ".pdf",
            BlobReference = "projects/proj-1/documents/doc-1/report.pdf",
        });

        await _handler.Handle(request, CancellationToken.None);

        await _storageClient.Received(1).DownloadObjectAsync(
            "test-bucket",
            "projects/proj-1/documents/doc-1/report.pdf",
            Arg.Any<Stream>(),
            Arg.Any<DownloadObjectOptions>(), Arg.Any<CancellationToken>(),
            Arg.Any<IProgress<IDownloadProgress>>());
    }

    [Test]
    public async Task Handle_UpdatesSearchBody_ForPdfFile()
    {
        SetupPdfDownload(CreateMinimalPdfBytes());
        var request = BuildRequest(new DocumentUploadedEvent
        {
            DocumentId    = "doc-1",
            FileExtension = ".pdf",
            BlobReference = "projects/proj-1/documents/doc-1/report.pdf",
        });

        await _handler.Handle(request, CancellationToken.None);

        await _repository.Received(1).UpdateSearchBodyAsync(
            _dbContext, "doc-1", Arg.Any<string>());
    }
}
