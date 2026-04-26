using Apotheca.Api.Events;
using Apotheca.Api.Events.Documents;
using Apotheca.Api.Features.Documents.RestoreDocument;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.RestoreDocument;

[TestFixture]
public class RestoreDocumentControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private RestoreDocumentRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private IEventPublisher _eventPublisher = null!;
    private RestoreDocumentController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<RestoreDocumentRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();
        _eventPublisher   = Substitute.For<IEventPublisher>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _repository.RestoreAncestorsAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IReadOnlyList<RestoredAncestor>>([]));

        _controller = new RestoreDocumentController(
            _dbContextFactory, _repository, _securityProvider, _eventPublisher,
            Substitute.For<ILogger<RestoreDocumentController>>());
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

    private void DenyProjectAccess(string errorMessage = "Access to this project was denied.")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure(errorMessage)));
    }

    private void DocumentExists(string title = "My Document", bool isFolder = false)
    {
        _repository.GetDeletedDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<DocumentInfo?>(new DocumentInfo(title, isFolder)));
    }

    // --- Security ---

    [Test]
    public async Task RestoreDocument_Returns401_WhenSecurityCheckFails()
    {
        DenyProjectAccess();

        var result = await _controller.RestoreDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task RestoreDocument_Returns401_WithSecurityProviderErrorMessage()
    {
        DenyProjectAccess("Access to this project was denied.");

        var result = (UnauthorizedObjectResult)await _controller.RestoreDocument("proj-1", "doc-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("Access to this project was denied."));
    }

    [Test]
    public async Task RestoreDocument_CallsSecurityProvider_WithCorrectProjectId()
    {
        DenyProjectAccess();

        await _controller.RestoreDocument("proj-xyz", "doc-1", CancellationToken.None);

        await _securityProvider.Received(1).AuthorizeProjectAccessAsync(_dbContext, "proj-xyz", Arg.Any<CancellationToken>());
    }

    // --- Document lookup ---

    [Test]
    public async Task RestoreDocument_Returns404_WhenDocumentDoesNotExist()
    {
        AllowProjectAccess();
        _repository.GetDeletedDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<DocumentInfo?>(null));

        var result = await _controller.RestoreDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task RestoreDocument_FetchesDocumentInfoWithCorrectProjectIdAndDocumentId()
    {
        AllowProjectAccess();
        _repository.GetDeletedDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<DocumentInfo?>(null));

        await _controller.RestoreDocument("proj-xyz", "doc-abc", CancellationToken.None);

        await _repository.Received(1).GetDeletedDocumentInfoAsync(_dbContext, "proj-xyz", "doc-abc");
    }

    // --- Success ---

    [Test]
    public async Task RestoreDocument_Returns204_WhenDocumentIsRestored()
    {
        AllowProjectAccess();
        DocumentExists();

        var result = await _controller.RestoreDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task RestoreDocument_CallsRestore_WithCorrectDocumentId()
    {
        AllowProjectAccess();
        DocumentExists();

        await _controller.RestoreDocument("proj-1", "doc-abc", CancellationToken.None);

        await _repository.Received(1).RestoreDocumentAsync(_dbContext, "doc-abc");
    }

    [Test]
    public async Task RestoreDocument_DoesNotRestore_WhenSecurityCheckFails()
    {
        DenyProjectAccess();

        await _controller.RestoreDocument("proj-1", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().RestoreDocumentAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    [Test]
    public async Task RestoreDocument_DoesNotRestore_WhenDocumentDoesNotExist()
    {
        AllowProjectAccess();
        _repository.GetDeletedDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<DocumentInfo?>(null));

        await _controller.RestoreDocument("proj-1", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().RestoreDocumentAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    // --- Transaction ---

    [Test]
    public async Task RestoreDocument_BeginsTransaction_BeforeWriting()
    {
        AllowProjectAccess();
        DocumentExists();

        await _controller.RestoreDocument("proj-1", "doc-1", CancellationToken.None);

        await _dbContext.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestoreDocument_CommitsTransaction_AfterWriting()
    {
        AllowProjectAccess();
        DocumentExists();

        await _controller.RestoreDocument("proj-1", "doc-1", CancellationToken.None);

        await _dbContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // --- Document log ---

    [Test]
    public async Task RestoreDocument_WritesDocumentLog_WhenDocumentIsRestored()
    {
        AllowProjectAccess("user-id-xyz");
        DocumentExists("My Document", isFolder: false);

        await _controller.RestoreDocument("proj-xyz", "doc-abc", CancellationToken.None);

        await _repository.Received(1).InsertDocumentLogAsync(_dbContext, "doc-abc", "user-id-xyz", "proj-xyz", "My Document", false);
    }

    [Test]
    public async Task RestoreDocument_WritesDocumentLog_WhenFolderIsRestored()
    {
        AllowProjectAccess("user-id-xyz");
        DocumentExists("My Folder", isFolder: true);

        await _controller.RestoreDocument("proj-xyz", "folder-abc", CancellationToken.None);

        await _repository.Received(1).InsertDocumentLogAsync(_dbContext, "folder-abc", "user-id-xyz", "proj-xyz", "My Folder", true);
    }

    [Test]
    public async Task RestoreDocument_DoesNotWriteDocumentLog_WhenSecurityCheckFails()
    {
        DenyProjectAccess();

        await _controller.RestoreDocument("proj-xyz", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().InsertDocumentLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    // --- Activity log ---

    [Test]
    public async Task RestoreDocument_WritesActivityLog_WithDocumentTitle()
    {
        AllowProjectAccess("user-id-xyz");
        DocumentExists("Spec.pdf", isFolder: false);

        await _controller.RestoreDocument("proj-xyz", "doc-abc", CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "doc-abc", "user-id-xyz", "Document 'Spec.pdf' restored");
    }

    [Test]
    public async Task RestoreDocument_WritesActivityLog_WithFolderTitle()
    {
        AllowProjectAccess("user-id-xyz");
        DocumentExists("Archive", isFolder: true);

        await _controller.RestoreDocument("proj-xyz", "folder-abc", CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "folder-abc", "user-id-xyz", "Folder 'Archive' restored");
    }

    [Test]
    public async Task RestoreDocument_DoesNotWriteActivityLog_WhenSecurityCheckFails()
    {
        DenyProjectAccess();

        await _controller.RestoreDocument("proj-xyz", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Ancestor restore ---

    [Test]
    public async Task RestoreDocument_CallsRestoreAncestors_WithCorrectDocumentId()
    {
        AllowProjectAccess();
        DocumentExists();

        await _controller.RestoreDocument("proj-1", "doc-abc", CancellationToken.None);

        await _repository.Received(1).RestoreAncestorsAsync(_dbContext, "doc-abc");
    }

    [Test]
    public async Task RestoreDocument_DoesNotCallRestoreAncestors_WhenDocumentDoesNotExist()
    {
        AllowProjectAccess();
        _repository.GetDeletedDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<DocumentInfo?>(null));

        await _controller.RestoreDocument("proj-1", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().RestoreAncestorsAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    // --- Event publishing ---

    [Test]
    public async Task RestoreDocument_PublishesDocumentRestoredEvent_WithCorrectDocumentId()
    {
        AllowProjectAccess();
        DocumentExists();

        await _controller.RestoreDocument("proj-1", "doc-abc", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            DocumentRestoredEvent.TopicId,
            Arg.Is<DocumentRestoredEvent>(e => e.DocumentId == "doc-abc"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestoreDocument_PublishesDocumentRestoredEvent_WithCorrectProjectIdAndUserId()
    {
        AllowProjectAccess("user-id-xyz");
        DocumentExists();

        await _controller.RestoreDocument("proj-xyz", "doc-1", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            DocumentRestoredEvent.TopicId,
            Arg.Is<DocumentRestoredEvent>(e => e.ProjectId == "proj-xyz" && e.UserId == "user-id-xyz"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestoreDocument_PublishesDocumentRestoredEvent_WithCorrectTitleAndIsFolder()
    {
        AllowProjectAccess();
        DocumentExists("My Folder", isFolder: true);

        await _controller.RestoreDocument("proj-1", "doc-1", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            DocumentRestoredEvent.TopicId,
            Arg.Is<DocumentRestoredEvent>(e => e.Title == "My Folder" && e.IsFolder == true),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestoreDocument_PublishesDocumentRestoredEvent_WithRestoredAncestors()
    {
        AllowProjectAccess();
        DocumentExists();

        var ancestors = new List<RestoredAncestor>
        {
            new() { DocumentId = "ancestor-1", Title = "Parent Folder", IsFolder = true },
        };
        _repository.RestoreAncestorsAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IReadOnlyList<RestoredAncestor>>(ancestors));

        await _controller.RestoreDocument("proj-1", "doc-1", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            DocumentRestoredEvent.TopicId,
            Arg.Is<DocumentRestoredEvent>(e => e.RestoredAncestors.Count == 1 && e.RestoredAncestors[0].DocumentId == "ancestor-1"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestoreDocument_DoesNotPublishEvent_WhenDocumentDoesNotExist()
    {
        AllowProjectAccess();
        _repository.GetDeletedDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<DocumentInfo?>(null));

        await _controller.RestoreDocument("proj-1", "doc-1", CancellationToken.None);

        await _eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<DocumentRestoredEvent>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestoreDocument_DoesNotPublishEvent_WhenSecurityCheckFails()
    {
        DenyProjectAccess();

        await _controller.RestoreDocument("proj-1", "doc-1", CancellationToken.None);

        await _eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<DocumentRestoredEvent>(), Arg.Any<CancellationToken>());
    }
}
