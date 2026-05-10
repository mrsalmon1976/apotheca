using Apotheca.Api.Configuration;
using Apotheca.Api.Events;
using Apotheca.Api.Events.Documents.DocumentUploaded;
using Apotheca.Api.Features.Documents.UploadDocument;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Google.Apis.Upload;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.UploadDocument;

[TestFixture]
public class UploadDocumentControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private IAppSettings _appSettings = null!;
    private StorageClient _storageClient = null!;
    private UploadDocumentRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private IEventPublisher _eventPublisher = null!;
    private UploadDocumentController _controller = null!;
    private IFormFile _file = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _appSettings      = Substitute.For<IAppSettings>();
        _storageClient    = Substitute.For<StorageClient>();
        _repository       = Substitute.For<UploadDocumentRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();
        _eventPublisher   = Substitute.For<IEventPublisher>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _appSettings.StorageBucketName.Returns("test-bucket");

        _file = Substitute.For<IFormFile>();
        _file.Length.Returns(1024L);
        _file.FileName.Returns("report.pdf");
        _file.ContentType.Returns("application/pdf");
        _file.OpenReadStream().Returns(new MemoryStream(new byte[1024]));

        _storageClient
            .UploadObjectAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
                Arg.Any<UploadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IUploadProgress>>())
            .Returns(Task.FromResult(new Google.Apis.Storage.v1.Data.Object()));

        _controller = new UploadDocumentController(
            _dbContextFactory,
            _appSettings,
            _storageClient,
            _repository,
            _securityProvider,
            _eventPublisher,
            Substitute.For<ILogger<UploadDocumentController>>());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _storageClient.Dispose();
    }

    private void AllowProjectAccess(string userId = "user-id-123")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("firebase-uid", userId)));
    }

    private void DenyProjectAccess(string errorMessage = "User does not have access to this project.")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure(errorMessage)));
    }

    private void SetupInsert(string documentId = "new-doc-id")
    {
        _repository
            .InsertDocumentWithIdAsync(
                _dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<long>(), Arg.Any<string>())
            .Returns(Task.FromResult(documentId));
    }

    // --- Identity / Access control ---

    [Test]
    public async Task UploadDocument_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.UploadDocument("proj-1", null, _file, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task UploadDocument_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.UploadDocument("proj-1", null, _file, null, CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- File validation ---

    [Test]
    public async Task UploadDocument_Returns400_WhenFileIsNull()
    {
        var result = await _controller.UploadDocument("proj-1", null, null!, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UploadDocument_Returns400_WhenFileLengthIsZero()
    {
        _file.Length.Returns(0L);

        var result = await _controller.UploadDocument("proj-1", null, _file, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UploadDocument_Returns400_WithErrorMessage_WhenFileIsInvalid()
    {
        var result = (BadRequestObjectResult)await _controller.UploadDocument("proj-1", null, null!, null, CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("No file provided."));
    }

    // --- Access control ---

    [Test]
    public async Task UploadDocument_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.UploadDocument("proj-1", null, _file, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- GCS upload ---

    [Test]
    public async Task UploadDocument_UploadsToCorrectBucket()
    {
        AllowProjectAccess();
        SetupInsert();

        await _controller.UploadDocument("proj-1", null, _file, "My Doc", CancellationToken.None);

        await _storageClient.Received(1).UploadObjectAsync(
            "test-bucket", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<UploadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IUploadProgress>>());
    }

    [Test]
    public async Task UploadDocument_UploadsWithObjectNameScopedToProjectAndDocument()
    {
        AllowProjectAccess();
        SetupInsert();
        _file.FileName.Returns("report.pdf");

        await _controller.UploadDocument("proj-xyz", null, _file, "My Doc", CancellationToken.None);

        await _storageClient.Received(1).UploadObjectAsync(
            Arg.Any<string>(),
            Arg.Is<string>(name => name.StartsWith("projects/proj-xyz/documents/") && name.EndsWith("/report.pdf")),
            Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<UploadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IUploadProgress>>());
    }

    [Test]
    public async Task UploadDocument_UploadsWithCorrectContentType()
    {
        AllowProjectAccess();
        SetupInsert();
        _file.ContentType.Returns("image/png");

        await _controller.UploadDocument("proj-1", null, _file, "My Doc", CancellationToken.None);

        await _storageClient.Received(1).UploadObjectAsync(
            Arg.Any<string>(), Arg.Any<string>(), "image/png", Arg.Any<Stream>(),
            Arg.Any<UploadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IUploadProgress>>());
    }

    [Test]
    public async Task UploadDocument_DoesNotUploadToStorage_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.UploadDocument("proj-1", null, _file, null, CancellationToken.None);

        await _storageClient.DidNotReceive().UploadObjectAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<UploadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IUploadProgress>>());
    }

    // --- Document insert ---

    [Test]
    public async Task UploadDocument_InsertsWithCorrectProjectIdAndUserId()
    {
        AllowProjectAccess("user-id-xyz");
        SetupInsert();

        await _controller.UploadDocument("proj-xyz", null, _file, "My Doc", CancellationToken.None);

        await _repository.Received(1).InsertDocumentWithIdAsync(
            _dbContext, Arg.Any<string>(), "proj-xyz", "user-id-xyz", Arg.Any<string?>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<long>(), Arg.Any<string>());
    }

    [Test]
    public async Task UploadDocument_InsertsWithProvidedTitle()
    {
        AllowProjectAccess();
        SetupInsert();

        await _controller.UploadDocument("proj-1", null, _file, "Custom Title", CancellationToken.None);

        await _repository.Received(1).InsertDocumentWithIdAsync(
            _dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            "Custom Title", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<long>(), Arg.Any<string>());
    }

    [Test]
    public async Task UploadDocument_UseFilenameWithoutExtension_WhenTitleIsNull()
    {
        AllowProjectAccess();
        SetupInsert();
        _file.FileName.Returns("quarterly-report.pdf");

        await _controller.UploadDocument("proj-1", null, _file, null, CancellationToken.None);

        await _repository.Received(1).InsertDocumentWithIdAsync(
            _dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            "quarterly-report", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<long>(), Arg.Any<string>());
    }

    [Test]
    public async Task UploadDocument_UseFilenameWithoutExtension_WhenTitleIsWhitespace()
    {
        AllowProjectAccess();
        SetupInsert();
        _file.FileName.Returns("quarterly-report.pdf");

        await _controller.UploadDocument("proj-1", null, _file, "   ", CancellationToken.None);

        await _repository.Received(1).InsertDocumentWithIdAsync(
            _dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            "quarterly-report", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<long>(), Arg.Any<string>());
    }

    [Test]
    public async Task UploadDocument_ForwardsParentId_ToRepository()
    {
        AllowProjectAccess();
        SetupInsert();

        await _controller.UploadDocument("proj-1", "folder-id-456", _file, "My Doc", CancellationToken.None);

        await _repository.Received(1).InsertDocumentWithIdAsync(
            _dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), "folder-id-456",
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<long>(), Arg.Any<string>());
    }

    [Test]
    public async Task UploadDocument_PassesNullParentId_WhenNotProvided()
    {
        AllowProjectAccess();
        SetupInsert();

        await _controller.UploadDocument("proj-1", null, _file, "My Doc", CancellationToken.None);

        await _repository.Received(1).InsertDocumentWithIdAsync(
            _dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Is<string?>(x => x == null),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<long>(), Arg.Any<string>());
    }

    // --- Document log ---

    [Test]
    public async Task UploadDocument_WritesDocumentLog_WhenUploadSucceeds()
    {
        AllowProjectAccess("user-id-xyz");
        SetupInsert("new-doc-id");

        await _controller.UploadDocument("proj-xyz", null, _file, "My Doc", CancellationToken.None);

        await _repository.Received(1).InsertDocumentLogAsync(_dbContext, "new-doc-id", "user-id-xyz", "proj-xyz");
    }

    [Test]
    public async Task UploadDocument_DoesNotWriteDocumentLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.UploadDocument("proj-xyz", null, _file, null, CancellationToken.None);

        await _repository.DidNotReceive().InsertDocumentLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Activity log ---

    [Test]
    public async Task UploadDocument_WritesProjectActivityLog_WhenUploadSucceeds()
    {
        AllowProjectAccess("user-id-xyz");
        SetupInsert("new-doc-id");

        await _controller.UploadDocument("proj-xyz", null, _file, "My Doc", CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "new-doc-id", "user-id-xyz", "Document uploaded");
    }

    [Test]
    public async Task UploadDocument_DoesNotWriteActivityLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.UploadDocument("proj-xyz", null, _file, null, CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Search index ---

    [Test]
    public async Task UploadDocument_UpsertSearch_WithProvidedTitle()
    {
        AllowProjectAccess();
        SetupInsert("new-doc-id");

        await _controller.UploadDocument("proj-xyz", null, _file, "My Report", CancellationToken.None);

        await _repository.Received(1).UpsertSearchAsync(_dbContext, "proj-xyz", "new-doc-id", "My Report");
    }

    [Test]
    public async Task UploadDocument_UpsertSearch_WithFilenameTitle_WhenTitleIsNull()
    {
        AllowProjectAccess();
        SetupInsert("new-doc-id");
        _file.FileName.Returns("quarterly-report.pdf");

        await _controller.UploadDocument("proj-xyz", null, _file, null, CancellationToken.None);

        await _repository.Received(1).UpsertSearchAsync(_dbContext, "proj-xyz", "new-doc-id", "quarterly-report");
    }

    [Test]
    public async Task UploadDocument_DoesNotUpsertSearch_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.UploadDocument("proj-xyz", null, _file, null, CancellationToken.None);

        await _repository.DidNotReceive().UpsertSearchAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Response ---

    [Test]
    public async Task UploadDocument_Returns201_WhenUploadSucceeds()
    {
        AllowProjectAccess();
        SetupInsert();

        var result = await _controller.UploadDocument("proj-1", null, _file, "My Doc", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task UploadDocument_ReturnsDocumentId_WhenUploadSucceeds()
    {
        AllowProjectAccess();
        SetupInsert("uploaded-doc-id");

        var result = (CreatedAtActionResult)await _controller.UploadDocument("proj-1", null, _file, "My Doc", CancellationToken.None);
        var id     = result.Value?.GetType().GetProperty("id")?.GetValue(result.Value)?.ToString();

        Assert.That(id, Is.EqualTo("uploaded-doc-id"));
    }

    // --- Event publishing ---

    [Test]
    public async Task UploadDocument_PublishesDocumentUploadedEvent_WithCorrectDocumentId()
    {
        AllowProjectAccess();
        SetupInsert("new-doc-id");

        await _controller.UploadDocument("proj-1", null, _file, "My Doc", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            DocumentUploadedEvent.TopicId,
            Arg.Is<DocumentUploadedEvent>(e => e.DocumentId == "new-doc-id"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UploadDocument_PublishesDocumentUploadedEvent_WithCorrectProjectId()
    {
        AllowProjectAccess();
        SetupInsert();

        await _controller.UploadDocument("proj-xyz", null, _file, "My Doc", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            DocumentUploadedEvent.TopicId,
            Arg.Is<DocumentUploadedEvent>(e => e.ProjectId == "proj-xyz"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UploadDocument_PublishesDocumentUploadedEvent_WithCorrectBlobReference()
    {
        AllowProjectAccess();
        SetupInsert("doc-id-123");
        _file.FileName.Returns("report.pdf");

        await _controller.UploadDocument("proj-xyz", null, _file, "My Doc", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            DocumentUploadedEvent.TopicId,
            Arg.Is<DocumentUploadedEvent>(e =>
                e.BlobReference.StartsWith("projects/proj-xyz/documents/") &&
                e.BlobReference.EndsWith("/report.pdf")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UploadDocument_PublishesDocumentUploadedEvent_WithCorrectFileExtension()
    {
        AllowProjectAccess();
        SetupInsert();
        _file.FileName.Returns("report.pdf");

        await _controller.UploadDocument("proj-xyz", null, _file, "My Doc", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            DocumentUploadedEvent.TopicId,
            Arg.Is<DocumentUploadedEvent>(e => e.FileExtension == ".pdf"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UploadDocument_DoesNotPublishEvent_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.UploadDocument("proj-1", null, _file, null, CancellationToken.None);

        await _eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<DocumentUploadedEvent>(), Arg.Any<CancellationToken>());
    }
}
