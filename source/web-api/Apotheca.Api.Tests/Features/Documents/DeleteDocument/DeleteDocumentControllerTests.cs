using System.Security.Claims;
using Apotheca.Api.Events;
using Apotheca.Api.Events.Documents;
using Apotheca.Api.Features.Documents.DeleteDocument;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.DeleteDocument;

[TestFixture]
public class DeleteDocumentControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private DeleteDocumentRepository _repository = null!;
    private IEventPublisher _eventPublisher = null!;
    private DeleteDocumentController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<DeleteDocumentRepository>();
        _eventPublisher   = Substitute.For<IEventPublisher>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new DeleteDocumentController(_dbContextFactory, _repository, _eventPublisher, Substitute.For<ILogger<DeleteDocumentController>>());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private void SetAuthenticatedUser(string firebaseUid)
    {
        var claims   = new[] { new Claim("sub", firebaseUid) };
        var identity = new ClaimsIdentity(claims, "test");
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
    }

    private void AllowProjectAccess(string userId = "user-id-123")
    {
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        _repository.GetUserIdAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<string?>(userId));
    }

    private void DocumentExists(string title = "My Document", bool isFolder = false)
    {
        _repository.GetDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<DocumentInfo?>(new DocumentInfo(title, isFolder)));
    }

    // --- Identity ---

    [Test]
    public async Task DeleteDocument_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task DeleteDocument_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task DeleteDocument_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task DeleteDocument_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.DeleteDocument("proj-xyz", "doc-1", CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    // --- User lookup ---

    [Test]
    public async Task DeleteDocument_Returns401_WhenUserIdCannotBeResolved()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        _repository.GetUserIdAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<string?>(null));

        var result = await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Document lookup ---

    [Test]
    public async Task DeleteDocument_Returns404_WhenDocumentDoesNotExist()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<DocumentInfo?>(null));

        var result = await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DeleteDocument_FetchesDocumentInfoWithCorrectProjectIdAndDocumentId()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<DocumentInfo?>(null));

        await _controller.DeleteDocument("proj-xyz", "doc-abc", CancellationToken.None);

        await _repository.Received(1).GetDocumentInfoAsync(_dbContext, "proj-xyz", "doc-abc");
    }

    // --- Success ---

    [Test]
    public async Task DeleteDocument_Returns204_WhenDocumentIsDeleted()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        DocumentExists();

        var result = await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteDocument_CallsSoftDelete_WithCorrectDocumentId()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        DocumentExists();

        await _controller.DeleteDocument("proj-1", "doc-abc", CancellationToken.None);

        await _repository.Received(1).SoftDeleteDocumentAsync(_dbContext, "doc-abc");
    }

    [Test]
    public async Task DeleteDocument_DoesNotSoftDelete_WhenAccessIsDenied()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().SoftDeleteDocumentAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    [Test]
    public async Task DeleteDocument_DoesNotSoftDelete_WhenDocumentDoesNotExist()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        _repository.GetDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<DocumentInfo?>(null));

        await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().SoftDeleteDocumentAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    // --- Transaction ---

    [Test]
    public async Task DeleteDocument_BeginsTransaction_BeforeWriting()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        DocumentExists();

        await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        await _dbContext.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteDocument_CommitsTransaction_AfterWriting()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        DocumentExists();

        await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        await _dbContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // --- Document log ---

    [Test]
    public async Task DeleteDocument_WritesDocumentLog_WhenDocumentIsDeleted()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        DocumentExists("My Document", isFolder: false);

        await _controller.DeleteDocument("proj-xyz", "doc-abc", CancellationToken.None);

        await _repository.Received(1).InsertDocumentLogAsync(_dbContext, "doc-abc", "user-id-xyz", "proj-xyz", "My Document", false);
    }

    [Test]
    public async Task DeleteDocument_WritesDocumentLog_WhenFolderIsDeleted()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        DocumentExists("My Folder", isFolder: true);

        await _controller.DeleteDocument("proj-xyz", "folder-abc", CancellationToken.None);

        await _repository.Received(1).InsertDocumentLogAsync(_dbContext, "folder-abc", "user-id-xyz", "proj-xyz", "My Folder", true);
    }

    [Test]
    public async Task DeleteDocument_DoesNotWriteDocumentLog_WhenAccessIsDenied()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.DeleteDocument("proj-xyz", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().InsertDocumentLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    // --- Activity log ---

    [Test]
    public async Task DeleteDocument_WritesActivityLog_WithDocumentTitle()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        DocumentExists("Spec.pdf", isFolder: false);

        await _controller.DeleteDocument("proj-xyz", "doc-abc", CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "doc-abc", "user-id-xyz", "Document 'Spec.pdf' deleted");
    }

    [Test]
    public async Task DeleteDocument_WritesActivityLog_WithFolderTitle()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        DocumentExists("Archive", isFolder: true);

        await _controller.DeleteDocument("proj-xyz", "folder-abc", CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "folder-abc", "user-id-xyz", "Folder 'Archive' deleted");
    }

    [Test]
    public async Task DeleteDocument_DoesNotWriteActivityLog_WhenAccessIsDenied()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.DeleteDocument("proj-xyz", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Event publishing ---

    [Test]
    public async Task DeleteDocument_PublishesDocumentDeletedEvent_WithCorrectDocumentId()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        DocumentExists();

        await _controller.DeleteDocument("proj-1", "doc-abc", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            DocumentDeletedEvent.TopicId,
            Arg.Is<DocumentDeletedEvent>(e => e.DocumentId == "doc-abc"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteDocument_PublishesDocumentDeletedEvent_WithCorrectProjectIdAndUserId()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        DocumentExists();

        await _controller.DeleteDocument("proj-xyz", "doc-1", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            DocumentDeletedEvent.TopicId,
            Arg.Is<DocumentDeletedEvent>(e => e.ProjectId == "proj-xyz" && e.UserId == "user-id-xyz"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteDocument_PublishesDocumentDeletedEvent_WithCorrectTitleAndIsFolder()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        DocumentExists("My Folder", isFolder: true);

        await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            DocumentDeletedEvent.TopicId,
            Arg.Is<DocumentDeletedEvent>(e => e.Title == "My Folder" && e.IsFolder == true),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteDocument_DoesNotPublishEvent_WhenDocumentDoesNotExist()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        _repository.GetDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<DocumentInfo?>(null));

        await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        await _eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<DocumentDeletedEvent>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteDocument_DoesNotPublishEvent_WhenAccessIsDenied()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        await _eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<DocumentDeletedEvent>(), Arg.Any<CancellationToken>());
    }
}
