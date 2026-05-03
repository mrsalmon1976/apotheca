using Apotheca.Api.Configuration;
using Apotheca.Api.Features.Notes.GetNoteAttachment;
using Apotheca.Data;
using Google.Apis.Download;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Notes.GetNoteAttachment;

[TestFixture]
public class GetNoteAttachmentControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private IAppSettings _appSettings = null!;
    private StorageClient _storageClient = null!;
    private GetNoteAttachmentRepository _repository = null!;
    private GetNoteAttachmentController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _appSettings      = Substitute.For<IAppSettings>();
        _storageClient    = Substitute.For<StorageClient>();
        _repository       = Substitute.For<GetNoteAttachmentRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _appSettings.StorageBucketName.Returns("test-bucket");

        _controller = new GetNoteAttachmentController(
            _dbContextFactory,
            _appSettings,
            _storageClient,
            _repository);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _storageClient.Dispose();
    }

    private void SetupAttachment(
        string attachmentId = "att-1",
        string blobReference = "projects/proj-1/notes/note-1/att-1.png",
        string fileName = "screenshot.png",
        string mimetype = "image/png")
    {
        _repository
            .GetAttachmentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetNoteAttachmentRepository.NoteAttachment?>(
                new GetNoteAttachmentRepository.NoteAttachment(attachmentId, blobReference, fileName, mimetype)));

        // The string-based DownloadObjectAsync overload delegates internally to the Object-based abstract virtual method.
        _storageClient
            .DownloadObjectAsync(
                Arg.Any<Google.Apis.Storage.v1.Data.Object>(), Arg.Any<Stream>(),
                Arg.Any<DownloadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IDownloadProgress>>())
            .Returns(Task.FromResult(new Google.Apis.Storage.v1.Data.Object()));
    }

    // --- Not found ---

    [Test]
    public async Task GetNoteAttachment_Returns404_WhenAttachmentDoesNotExist()
    {
        _repository
            .GetAttachmentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetNoteAttachmentRepository.NoteAttachment?>(null));

        var result = await _controller.GetNoteAttachment("proj-1", "att-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetNoteAttachment_QueriesWithCorrectProjectIdAndAttachmentId()
    {
        _repository
            .GetAttachmentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetNoteAttachmentRepository.NoteAttachment?>(null));

        await _controller.GetNoteAttachment("proj-xyz", "att-abc", CancellationToken.None);

        await _repository.Received(1).GetAttachmentAsync(_dbContext, "proj-xyz", "att-abc");
    }

    [Test]
    public async Task GetNoteAttachment_DoesNotDownloadFromStorage_WhenAttachmentNotFound()
    {
        _repository
            .GetAttachmentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetNoteAttachmentRepository.NoteAttachment?>(null));

        await _controller.GetNoteAttachment("proj-1", "att-1", CancellationToken.None);

        await _storageClient.DidNotReceive().DownloadObjectAsync(
            Arg.Any<Google.Apis.Storage.v1.Data.Object>(), Arg.Any<Stream>(),
            Arg.Any<DownloadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IDownloadProgress>>());
    }

    // --- GCS download ---

    [Test]
    public async Task GetNoteAttachment_DownloadsFromCorrectBucket()
    {
        SetupAttachment(blobReference: "projects/proj-1/notes/note-1/att-1.png");

        await _controller.GetNoteAttachment("proj-1", "att-1", CancellationToken.None);

        await _storageClient.Received(1).DownloadObjectAsync(
            Arg.Is<Google.Apis.Storage.v1.Data.Object>(o =>
                o.Bucket == "test-bucket" && o.Name == "projects/proj-1/notes/note-1/att-1.png"),
            Arg.Any<Stream>(),
            Arg.Any<DownloadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IDownloadProgress>>());
    }

    // --- Response ---

    [Test]
    public async Task GetNoteAttachment_ReturnsFileStreamResult_WhenSuccessful()
    {
        SetupAttachment();

        var result = await _controller.GetNoteAttachment("proj-1", "att-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<FileStreamResult>());
    }

    [Test]
    public async Task GetNoteAttachment_ReturnsCorrectContentType()
    {
        SetupAttachment(mimetype: "image/jpeg");

        var result = (FileStreamResult)await _controller.GetNoteAttachment("proj-1", "att-1", CancellationToken.None);

        Assert.That(result.ContentType, Is.EqualTo("image/jpeg"));
    }

    [Test]
    public async Task GetNoteAttachment_ReturnsCorrectFileName()
    {
        SetupAttachment(fileName: "diagram.jpg");

        var result = (FileStreamResult)await _controller.GetNoteAttachment("proj-1", "att-1", CancellationToken.None);

        Assert.That(result.FileDownloadName, Is.EqualTo("diagram.jpg"));
    }
}
