using Apotheca.Api.Configuration;
using Apotheca.Api.Features.Notes.SaveNoteAttachment;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Google.Apis.Upload;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Notes.SaveNoteAttachment;

[TestFixture]
public class SaveNoteAttachmentControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private IAppSettings _appSettings = null!;
    private StorageClient _storageClient = null!;
    private SaveNoteAttachmentRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private SaveNoteAttachmentController _controller = null!;
    private IFormFile _file = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _appSettings      = Substitute.For<IAppSettings>();
        _storageClient    = Substitute.For<StorageClient>();
        _repository       = Substitute.For<SaveNoteAttachmentRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _appSettings.StorageBucketName.Returns("test-bucket");

        _file = Substitute.For<IFormFile>();
        _file.Length.Returns(2048L);
        _file.FileName.Returns("screenshot.png");
        _file.ContentType.Returns("image/png");
        _file.OpenReadStream().Returns(new MemoryStream(new byte[2048]));

        _storageClient
            .UploadObjectAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
                Arg.Any<UploadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IUploadProgress>>())
            .Returns(Task.FromResult(new Google.Apis.Storage.v1.Data.Object()));

        _controller = new SaveNoteAttachmentController(
            _dbContextFactory,
            _appSettings,
            _storageClient,
            _repository,
            _securityProvider,
            Substitute.For<ILogger<SaveNoteAttachmentController>>());
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

    private void NoteExists(bool exists = true)
    {
        _repository.NoteExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(exists));
    }

    // --- File validation ---

    [Test]
    public async Task SaveNoteAttachment_Returns400_WhenFileIsNull()
    {
        var result = await _controller.SaveNoteAttachment("proj-1", "note-1", null!, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SaveNoteAttachment_Returns400_WhenFileLengthIsZero()
    {
        _file.Length.Returns(0L);

        var result = await _controller.SaveNoteAttachment("proj-1", "note-1", _file, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SaveNoteAttachment_Returns400_WithErrorMessage_WhenFileIsInvalid()
    {
        var result = (BadRequestObjectResult)await _controller.SaveNoteAttachment("proj-1", "note-1", null!, CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("No file provided."));
    }

    // --- Identity / Access control ---

    [Test]
    public async Task SaveNoteAttachment_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.SaveNoteAttachment("proj-1", "note-1", _file, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SaveNoteAttachment_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.SaveNoteAttachment("proj-1", "note-1", _file, CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task SaveNoteAttachment_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.SaveNoteAttachment("proj-1", "note-1", _file, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Note existence ---

    [Test]
    public async Task SaveNoteAttachment_Returns404_WhenNoteDoesNotExist()
    {
        AllowProjectAccess();
        NoteExists(false);

        var result = await _controller.SaveNoteAttachment("proj-1", "note-1", _file, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task SaveNoteAttachment_DoesNotUploadToStorage_WhenNoteDoesNotExist()
    {
        AllowProjectAccess();
        NoteExists(false);

        await _controller.SaveNoteAttachment("proj-1", "note-1", _file, CancellationToken.None);

        await _storageClient.DidNotReceive().UploadObjectAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<UploadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IUploadProgress>>());
    }

    // --- GCS upload ---

    [Test]
    public async Task SaveNoteAttachment_UploadsToCorrectBucket()
    {
        AllowProjectAccess();
        NoteExists();

        await _controller.SaveNoteAttachment("proj-1", "note-1", _file, CancellationToken.None);

        await _storageClient.Received(1).UploadObjectAsync(
            "test-bucket", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<UploadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IUploadProgress>>());
    }

    [Test]
    public async Task SaveNoteAttachment_UploadsWithObjectNameScopedToProjectNoteAndAttachment()
    {
        AllowProjectAccess();
        NoteExists();
        _file.FileName.Returns("diagram.png");

        await _controller.SaveNoteAttachment("proj-xyz", "note-abc", _file, CancellationToken.None);

        await _storageClient.Received(1).UploadObjectAsync(
            Arg.Any<string>(),
            Arg.Is<string>(name => name.StartsWith("projects/proj-xyz/notes/note-abc/") && name.EndsWith(".png")),
            Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<UploadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IUploadProgress>>());
    }

    [Test]
    public async Task SaveNoteAttachment_UploadsWithCorrectContentType()
    {
        AllowProjectAccess();
        NoteExists();
        _file.ContentType.Returns("image/jpeg");

        await _controller.SaveNoteAttachment("proj-1", "note-1", _file, CancellationToken.None);

        await _storageClient.Received(1).UploadObjectAsync(
            Arg.Any<string>(), Arg.Any<string>(), "image/jpeg", Arg.Any<Stream>(),
            Arg.Any<UploadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IUploadProgress>>());
    }

    [Test]
    public async Task SaveNoteAttachment_DoesNotUploadToStorage_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.SaveNoteAttachment("proj-1", "note-1", _file, CancellationToken.None);

        await _storageClient.DidNotReceive().UploadObjectAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<UploadObjectOptions>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<IUploadProgress>>());
    }

    // --- Repository insert ---

    [Test]
    public async Task SaveNoteAttachment_InsertsWithCorrectProjectNoteAndUserId()
    {
        AllowProjectAccess("user-id-xyz");
        NoteExists();

        await _controller.SaveNoteAttachment("proj-xyz", "note-abc", _file, CancellationToken.None);

        await _repository.Received(1).InsertNoteAttachmentAsync(
            _dbContext, Arg.Any<string>(), "proj-xyz", "note-abc",
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), "user-id-xyz");
    }

    [Test]
    public async Task SaveNoteAttachment_InsertsWithCorrectFileMetadata()
    {
        AllowProjectAccess();
        NoteExists();
        _file.FileName.Returns("photo.jpg");
        _file.ContentType.Returns("image/jpeg");
        _file.Length.Returns(4096L);

        await _controller.SaveNoteAttachment("proj-1", "note-1", _file, CancellationToken.None);

        await _repository.Received(1).InsertNoteAttachmentAsync(
            _dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), "photo.jpg", "image/jpeg", 4096L, Arg.Any<string>());
    }

    [Test]
    public async Task SaveNoteAttachment_DoesNotInsert_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.SaveNoteAttachment("proj-1", "note-1", _file, CancellationToken.None);

        await _repository.DidNotReceive().InsertNoteAttachmentAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string>());
    }

    // --- Response ---

    [Test]
    public async Task SaveNoteAttachment_Returns200_WhenUploadSucceeds()
    {
        AllowProjectAccess();
        NoteExists();

        var result = await _controller.SaveNoteAttachment("proj-1", "note-1", _file, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task SaveNoteAttachment_ReturnsAttachmentUrl_ContainingProjectAndAttachmentId()
    {
        AllowProjectAccess();
        NoteExists();

        var result = (OkObjectResult)await _controller.SaveNoteAttachment("proj-xyz", "note-1", _file, CancellationToken.None);
        var url    = result.Value?.GetType().GetProperty("url")?.GetValue(result.Value)?.ToString();

        Assert.That(url, Does.StartWith("/projects/proj-xyz/notes/attachments/"));
    }

    [Test]
    public async Task SaveNoteAttachment_ReturnsAttachmentId_InResponse()
    {
        AllowProjectAccess();
        NoteExists();

        var result = (OkObjectResult)await _controller.SaveNoteAttachment("proj-1", "note-1", _file, CancellationToken.None);
        var id     = result.Value?.GetType().GetProperty("id")?.GetValue(result.Value)?.ToString();

        Assert.That(id, Is.Not.Null.And.Not.Empty);
    }
}
