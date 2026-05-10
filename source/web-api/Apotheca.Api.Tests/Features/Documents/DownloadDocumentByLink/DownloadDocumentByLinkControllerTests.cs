using Apotheca.Api.Configuration;
using Apotheca.Api.Features.Documents.DownloadDocumentByLink;
using Apotheca.Data;
using Google.Apis.Download;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.DownloadDocumentByLink;

[TestFixture]
public class DownloadDocumentByLinkControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private IAppSettings _appSettings = null!;
    private StorageClient _storageClient = null!;
    private DownloadDocumentByLinkRepository _repository = null!;
    private DownloadDocumentByLinkController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _appSettings      = Substitute.For<IAppSettings>();
        _storageClient    = Substitute.For<StorageClient>();
        _repository       = Substitute.For<DownloadDocumentByLinkRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _appSettings.StorageBucketName.Returns("test-bucket");

        _controller = new DownloadDocumentByLinkController(
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

    private void SetupDownloadInfo(
        string blobRef  = "proj-1/doc-1/file.pdf",
        string fileName = "file.pdf",
        string mimetype = "application/pdf")
    {
        _repository
            .GetDownloadInfoAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<(string, string, string)?>(
                (blobRef, fileName, mimetype)));

        _storageClient
            .DownloadObjectAsync(
                Arg.Any<Google.Apis.Storage.v1.Data.Object>(), Arg.Any<Stream>(),
                Arg.Any<DownloadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IDownloadProgress>>())
            .Returns(Task.FromResult(new Google.Apis.Storage.v1.Data.Object()));
    }

    // --- Not found ---

    [Test]
    public async Task DownloadDocumentByLink_Returns404_WhenLinkDoesNotExist()
    {
        _repository
            .GetDownloadInfoAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<(string, string, string)?>(null));

        var result = await _controller.DownloadDocumentByLink("link-abc", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DownloadDocumentByLink_QueriesWithCorrectLinkId()
    {
        _repository
            .GetDownloadInfoAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<(string, string, string)?>(null));

        await _controller.DownloadDocumentByLink("link-xyz", CancellationToken.None);

        await _repository.Received(1).GetDownloadInfoAsync(_dbContext, "link-xyz");
    }

    // --- Success ---

    [Test]
    public async Task DownloadDocumentByLink_ReturnsFileStreamResult_WhenSuccessful()
    {
        SetupDownloadInfo();

        var result = await _controller.DownloadDocumentByLink("link-abc", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<FileStreamResult>());
    }

    [Test]
    public async Task DownloadDocumentByLink_ReturnsCorrectContentType()
    {
        SetupDownloadInfo(mimetype: "image/png");

        var result = (FileStreamResult)await _controller.DownloadDocumentByLink("link-abc", CancellationToken.None);

        Assert.That(result.ContentType, Is.EqualTo("image/png"));
    }

    [Test]
    public async Task DownloadDocumentByLink_ReturnsCorrectFileName()
    {
        SetupDownloadInfo(fileName: "quarterly-report.xlsx");

        var result = (FileStreamResult)await _controller.DownloadDocumentByLink("link-abc", CancellationToken.None);

        Assert.That(result.FileDownloadName, Is.EqualTo("quarterly-report.xlsx"));
    }
}
