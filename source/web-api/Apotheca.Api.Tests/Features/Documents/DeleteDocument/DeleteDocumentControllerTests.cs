using Apotheca.Api.Events;
using Apotheca.Api.Events.Documents.DocumentDeleted;
using Apotheca.Api.Features.Documents.DeleteDocument;
using Apotheca.Api.Providers;
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
    private ISecurityProvider _securityProvider = null!;
    private IEventPublisher _eventPublisher = null!;
    private DeleteDocumentController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<DeleteDocumentRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();
        _eventPublisher   = Substitute.For<IEventPublisher>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new DeleteDocumentController(_dbContextFactory, _repository, _securityProvider, _eventPublisher, Substitute.For<ILogger<DeleteDocumentController>>());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

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

    private void DocumentExists(string title = "My Document", bool isFolder = false)
    {
        _repository.GetDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<DocumentInfo?>(new DocumentInfo(title, isFolder)));
    }

    // --- Identity / Access control ---

    [Test]
    public async Task DeleteDocument_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task DeleteDocument_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task DeleteDocument_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Document lookup ---

    [Test]
    public async Task DeleteDocument_Returns404_WhenDocumentDoesNotExist()
    {
        AllowProjectAccess();
        _repository.GetDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<DocumentInfo?>(null));

        var result = await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DeleteDocument_FetchesDocumentInfoWithCorrectProjectIdAndDocumentId()
    {
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
        AllowProjectAccess();
        DocumentExists();

        var result = await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteDocument_CallsSoftDelete_WithCorrectDocumentId()
    {
        AllowProjectAccess();
        DocumentExists();

        await _controller.DeleteDocument("proj-1", "doc-abc", CancellationToken.None);

        await _repository.Received(1).SoftDeleteDocumentAsync(_dbContext, "doc-abc");
    }

    [Test]
    public async Task DeleteDocument_DoesNotSoftDelete_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().SoftDeleteDocumentAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    [Test]
    public async Task DeleteDocument_DoesNotSoftDelete_WhenDocumentDoesNotExist()
    {
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
        AllowProjectAccess();
        DocumentExists();

        await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        await _dbContext.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteDocument_CommitsTransaction_AfterWriting()
    {
        AllowProjectAccess();
        DocumentExists();

        await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        await _dbContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // --- Document log ---

    [Test]
    public async Task DeleteDocument_WritesDocumentLog_WhenDocumentIsDeleted()
    {
        AllowProjectAccess("user-id-xyz");
        DocumentExists("My Document", isFolder: false);

        await _controller.DeleteDocument("proj-xyz", "doc-abc", CancellationToken.None);

        await _repository.Received(1).InsertDocumentLogAsync(_dbContext, "doc-abc", "user-id-xyz", "proj-xyz", "My Document", false);
    }

    [Test]
    public async Task DeleteDocument_WritesDocumentLog_WhenFolderIsDeleted()
    {
        AllowProjectAccess("user-id-xyz");
        DocumentExists("My Folder", isFolder: true);

        await _controller.DeleteDocument("proj-xyz", "folder-abc", CancellationToken.None);

        await _repository.Received(1).InsertDocumentLogAsync(_dbContext, "folder-abc", "user-id-xyz", "proj-xyz", "My Folder", true);
    }

    [Test]
    public async Task DeleteDocument_DoesNotWriteDocumentLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.DeleteDocument("proj-xyz", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().InsertDocumentLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    // --- Activity log ---

    [Test]
    public async Task DeleteDocument_WritesActivityLog_WithDocumentTitle()
    {
        AllowProjectAccess("user-id-xyz");
        DocumentExists("Spec.pdf", isFolder: false);

        await _controller.DeleteDocument("proj-xyz", "doc-abc", CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "doc-abc", "user-id-xyz", "Document 'Spec.pdf' deleted");
    }

    [Test]
    public async Task DeleteDocument_WritesActivityLog_WithFolderTitle()
    {
        AllowProjectAccess("user-id-xyz");
        DocumentExists("Archive", isFolder: true);

        await _controller.DeleteDocument("proj-xyz", "folder-abc", CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "folder-abc", "user-id-xyz", "Folder 'Archive' deleted");
    }

    [Test]
    public async Task DeleteDocument_DoesNotWriteActivityLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.DeleteDocument("proj-xyz", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Event publishing ---

    [Test]
    public async Task DeleteDocument_PublishesDocumentDeletedEvent_WithCorrectDocumentId()
    {
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
        DenyProjectAccess();

        await _controller.DeleteDocument("proj-1", "doc-1", CancellationToken.None);

        await _eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<DocumentDeletedEvent>(), Arg.Any<CancellationToken>());
    }
}
