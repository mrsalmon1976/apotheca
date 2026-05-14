using Apotheca.Api.Events;
using Apotheca.Api.Events.Notes.NoteRestored;
using Apotheca.Api.Features.Notes.RestoreNote;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Notes.RestoreNote;

[TestFixture]
public class RestoreNoteControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private RestoreNoteRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private IEventPublisher _eventPublisher = null!;
    private RestoreNoteController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<RestoreNoteRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();
        _eventPublisher   = Substitute.For<IEventPublisher>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _repository.RestoreAncestorsAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IReadOnlyList<RestoredAncestor>>([]));

        _controller = new RestoreNoteController(_dbContextFactory, _repository, _securityProvider, _eventPublisher, Substitute.For<ILogger<RestoreNoteController>>());
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

    private void NoteExists(string title = "My Note", bool isFolder = false)
    {
        _repository.GetDeletedNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<NoteInfo?>(new NoteInfo(title, isFolder)));
    }

    // --- Identity / Access control ---

    [Test]
    public async Task RestoreNote_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task RestoreNote_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task RestoreNote_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Note lookup ---

    [Test]
    public async Task RestoreNote_Returns404_WhenNoteDoesNotExist()
    {
        AllowProjectAccess();
        _repository.GetDeletedNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<NoteInfo?>(null));

        var result = await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task RestoreNote_FetchesNoteInfoWithCorrectProjectIdAndNoteId()
    {
        AllowProjectAccess();
        _repository.GetDeletedNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<NoteInfo?>(null));

        await _controller.RestoreNote("proj-xyz", "note-abc", CancellationToken.None);

        await _repository.Received(1).GetDeletedNoteInfoAsync(_dbContext, "proj-xyz", "note-abc");
    }

    // --- Success ---

    [Test]
    public async Task RestoreNote_Returns204_WhenNoteIsRestored()
    {
        AllowProjectAccess();
        NoteExists();

        var result = await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task RestoreNote_CallsRestore_WithCorrectNoteId()
    {
        AllowProjectAccess();
        NoteExists();

        await _controller.RestoreNote("proj-1", "note-abc", CancellationToken.None);

        await _repository.Received(1).RestoreNoteAsync(_dbContext, "note-abc");
    }

    [Test]
    public async Task RestoreNote_DoesNotRestore_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        await _repository.DidNotReceive().RestoreNoteAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    [Test]
    public async Task RestoreNote_DoesNotRestore_WhenNoteDoesNotExist()
    {
        AllowProjectAccess();
        _repository.GetDeletedNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<NoteInfo?>(null));

        await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        await _repository.DidNotReceive().RestoreNoteAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    // --- Transaction ---

    [Test]
    public async Task RestoreNote_BeginsTransaction_BeforeWriting()
    {
        AllowProjectAccess();
        NoteExists();

        await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        await _dbContext.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestoreNote_CommitsTransaction_AfterWriting()
    {
        AllowProjectAccess();
        NoteExists();

        await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        await _dbContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // --- Note log ---

    [Test]
    public async Task RestoreNote_WritesNoteLog_WhenNoteIsRestored()
    {
        AllowProjectAccess("user-id-xyz");
        NoteExists("My Note", isFolder: false);

        await _controller.RestoreNote("proj-xyz", "note-abc", CancellationToken.None);

        await _repository.Received(1).InsertNoteLogAsync(_dbContext, "note-abc", "user-id-xyz", "proj-xyz", "My Note", false);
    }

    [Test]
    public async Task RestoreNote_WritesNoteLog_WhenFolderIsRestored()
    {
        AllowProjectAccess("user-id-xyz");
        NoteExists("My Folder", isFolder: true);

        await _controller.RestoreNote("proj-xyz", "folder-abc", CancellationToken.None);

        await _repository.Received(1).InsertNoteLogAsync(_dbContext, "folder-abc", "user-id-xyz", "proj-xyz", "My Folder", true);
    }

    [Test]
    public async Task RestoreNote_DoesNotWriteNoteLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.RestoreNote("proj-xyz", "note-1", CancellationToken.None);

        await _repository.DidNotReceive().InsertNoteLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    // --- Activity log ---

    [Test]
    public async Task RestoreNote_WritesActivityLog_WithNoteTitle()
    {
        AllowProjectAccess("user-id-xyz");
        NoteExists("Meeting Notes", isFolder: false);

        await _controller.RestoreNote("proj-xyz", "note-abc", CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "note-abc", "user-id-xyz", "Note 'Meeting Notes' restored");
    }

    [Test]
    public async Task RestoreNote_WritesActivityLog_WithFolderTitle()
    {
        AllowProjectAccess("user-id-xyz");
        NoteExists("Project Docs", isFolder: true);

        await _controller.RestoreNote("proj-xyz", "folder-abc", CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "folder-abc", "user-id-xyz", "Folder 'Project Docs' restored");
    }

    [Test]
    public async Task RestoreNote_DoesNotWriteActivityLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.RestoreNote("proj-xyz", "note-1", CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Ancestor restore ---

    [Test]
    public async Task RestoreNote_CallsRestoreAncestors_WithCorrectNoteId()
    {
        AllowProjectAccess();
        NoteExists();

        await _controller.RestoreNote("proj-1", "note-abc", CancellationToken.None);

        await _repository.Received(1).RestoreAncestorsAsync(_dbContext, "note-abc");
    }

    [Test]
    public async Task RestoreNote_DoesNotCallRestoreAncestors_WhenNoteDoesNotExist()
    {
        AllowProjectAccess();
        _repository.GetDeletedNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<NoteInfo?>(null));

        await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        await _repository.DidNotReceive().RestoreAncestorsAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    // --- Event publishing ---

    [Test]
    public async Task RestoreNote_PublishesNoteRestoredEvent_WithCorrectNoteId()
    {
        AllowProjectAccess();
        NoteExists();

        await _controller.RestoreNote("proj-1", "note-abc", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            NoteRestoredEvent.TopicId,
            Arg.Is<NoteRestoredEvent>(e => e.NoteId == "note-abc"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestoreNote_PublishesNoteRestoredEvent_WithCorrectProjectIdAndUserId()
    {
        AllowProjectAccess("user-id-xyz");
        NoteExists();

        await _controller.RestoreNote("proj-xyz", "note-1", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            NoteRestoredEvent.TopicId,
            Arg.Is<NoteRestoredEvent>(e => e.ProjectId == "proj-xyz" && e.UserId == "user-id-xyz"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestoreNote_PublishesNoteRestoredEvent_WithCorrectTitleAndIsFolder()
    {
        AllowProjectAccess();
        NoteExists("My Folder", isFolder: true);

        await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            NoteRestoredEvent.TopicId,
            Arg.Is<NoteRestoredEvent>(e => e.Title == "My Folder" && e.IsFolder == true),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestoreNote_PublishesNoteRestoredEvent_WithRestoredAncestors()
    {
        AllowProjectAccess();
        NoteExists();

        var ancestors = new List<RestoredAncestor>
        {
            new() { NoteId = "ancestor-1", Title = "Parent Folder", IsFolder = true },
        };
        _repository.RestoreAncestorsAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IReadOnlyList<RestoredAncestor>>(ancestors));

        await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            NoteRestoredEvent.TopicId,
            Arg.Is<NoteRestoredEvent>(e => e.RestoredAncestors.Count == 1 && e.RestoredAncestors[0].NoteId == "ancestor-1"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestoreNote_DoesNotPublishEvent_WhenNoteDoesNotExist()
    {
        AllowProjectAccess();
        _repository.GetDeletedNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<NoteInfo?>(null));

        await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        await _eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<NoteRestoredEvent>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestoreNote_DoesNotPublishEvent_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        await _eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<NoteRestoredEvent>(), Arg.Any<CancellationToken>());
    }
}
