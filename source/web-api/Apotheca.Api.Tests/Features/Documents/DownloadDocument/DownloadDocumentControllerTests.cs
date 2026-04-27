using Apotheca.Api.Configuration;
using Apotheca.Api.Features.Documents.DownloadDocument;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Google.Apis.Download;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.DownloadDocument;

[TestFixture]
public class DownloadDocumentControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private IAppSettings _appSettings = null!;
    private StorageClient _storageClient = null!;
    private DownloadDocumentRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private DownloadDocumentController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _appSettings      = Substitute.For<IAppSettings>();
        _storageClient    = Substitute.For<StorageClient>();
        _repository       = Substitute.For<DownloadDocumentRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _appSettings.StorageBucketName.Returns("test-bucket");

        _controller = new DownloadDocumentController(
            _dbContextFactory,
            _appSettings,
            _storageClient,
            _repository,
            _securityProvider);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _storageClient.Dispose();
    }

    private void AllowProjectAccess()
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("firebase-uid", "user-id-123")));
    }

    private void DenyProjectAccess(string errorMessage = "User does not have access to this project.")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure(errorMessage)));
    }

    private void SetupDownloadInfo(
        string blobRef  = "proj-1/doc-1/file.pdf",
        string fileName = "file.pdf",
        string mimetype = "application/pdf")
    {
        _repository
            .GetDownloadInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<(string, string, string)?>(
                (blobRef, fileName, mimetype)));

        // StorageClient.DownloadObjectAsync (Object-based overload) is the abstract virtual method;
        // the string-based overload in the controller delegates through it internally.
        _storageClient
            .DownloadObjectAsync(
                Arg.Any<Google.Apis.Storage.v1.Data.Object>(), Arg.Any<Stream>(),
                Arg.Any<DownloadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IDownloadProgress>>())
            .Returns(Task.FromResult(new Google.Apis.Storage.v1.Data.Object()));
    }

    // --- Access control ---

    [Test]
    public async Task DownloadDocument_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.DownloadDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task DownloadDocument_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.DownloadDocument("proj-1", "doc-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task DownloadDocument_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.DownloadDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task DownloadDocument_DoesNotQueryRepository_WhenAccessDenied()
    {
        DenyProjectAccess();

        await _controller.DownloadDocument("proj-1", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().GetDownloadInfoAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Not found ---

    [Test]
    public async Task DownloadDocument_Returns404_WhenDocumentNotFound()
    {
        AllowProjectAccess();
        _repository
            .GetDownloadInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<(string, string, string)?>(null));

        var result = await _controller.DownloadDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DownloadDocument_QueriesWithCorrectProjectIdAndDocumentId()
    {
        AllowProjectAccess();
        _repository
            .GetDownloadInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<(string, string, string)?>(null));

        await _controller.DownloadDocument("proj-xyz", "doc-abc", CancellationToken.None);

        await _repository.Received(1).GetDownloadInfoAsync(_dbContext, "proj-xyz", "doc-abc");
    }

    // --- Response ---

    [Test]
    public async Task DownloadDocument_ReturnsFileStreamResult_WhenSuccessful()
    {
        AllowProjectAccess();
        SetupDownloadInfo();

        var result = await _controller.DownloadDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<FileStreamResult>());
    }

    [Test]
    public async Task DownloadDocument_ReturnsCorrectContentType()
    {
        AllowProjectAccess();
        SetupDownloadInfo(mimetype: "image/png");

        var result = (FileStreamResult)await _controller.DownloadDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result.ContentType, Is.EqualTo("image/png"));
    }

    [Test]
    public async Task DownloadDocument_ReturnsCorrectFileName()
    {
        AllowProjectAccess();
        SetupDownloadInfo(fileName: "quarterly-report.pdf");

        var result = (FileStreamResult)await _controller.DownloadDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result.FileDownloadName, Is.EqualTo("quarterly-report.pdf"));
    }
}
