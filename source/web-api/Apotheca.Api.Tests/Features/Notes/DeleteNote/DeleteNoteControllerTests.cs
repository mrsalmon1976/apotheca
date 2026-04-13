using System.Security.Claims;
using Apotheca.Api.Events;
using Apotheca.Api.Events.Notes;
using Apotheca.Api.Features.Notes.DeleteNote;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Notes.DeleteNote;

[TestFixture]
public class DeleteNoteControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private DeleteNoteRepository _repository = null!;
    private IEventPublisher _eventPublisher = null!;
    private DeleteNoteController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<DeleteNoteRepository>();
        _eventPublisher   = Substitute.For<IEventPublisher>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new DeleteNoteController(_dbContextFactory, _repository, _eventPublisher, Substitute.For<ILogger<DeleteNoteController>>());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

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

    private void NoteExists(string title = "My Note", bool isFolder = false)
    {
        _repository.GetNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<NoteInfo?>(new NoteInfo(title, isFolder)));
    }

    // --- Identity ---

    [Test]
    public async Task DeleteNote_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.DeleteNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task DeleteNote_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.DeleteNote("proj-1", "note-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task DeleteNote_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.DeleteNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task DeleteNote_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.DeleteNote("proj-xyz", "note-1", CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    // --- User lookup ---

    [Test]
    public async Task DeleteNote_Returns401_WhenUserIdCannotBeResolved()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        _repository.GetUserIdAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<string?>(null));

        var result = await _controller.DeleteNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Note lookup ---

    [Test]
    public async Task DeleteNote_Returns404_WhenNoteDoesNotExist()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<NoteInfo?>(null));

        var result = await _controller.DeleteNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DeleteNote_FetchesNoteInfoWithCorrectProjectIdAndNoteId()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<NoteInfo?>(null));

        await _controller.DeleteNote("proj-xyz", "note-abc", CancellationToken.None);

        await _repository.Received(1).GetNoteInfoAsync(_dbContext, "proj-xyz", "note-abc");
    }

    // --- Success ---

    [Test]
    public async Task DeleteNote_Returns204_WhenNoteIsDeleted()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        NoteExists();

        var result = await _controller.DeleteNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteNote_CallsSoftDelete_WithCorrectNoteId()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        NoteExists();

        await _controller.DeleteNote("proj-1", "note-abc", CancellationToken.None);

        await _repository.Received(1).SoftDeleteNoteAsync(_dbContext, "note-abc");
    }

    [Test]
    public async Task DeleteNote_DoesNotSoftDelete_WhenAccessIsDenied()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.DeleteNote("proj-1", "note-1", CancellationToken.None);

        await _repository.DidNotReceive().SoftDeleteNoteAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    [Test]
    public async Task DeleteNote_DoesNotSoftDelete_WhenNoteDoesNotExist()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        _repository.GetNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<NoteInfo?>(null));

        await _controller.DeleteNote("proj-1", "note-1", CancellationToken.None);

        await _repository.DidNotReceive().SoftDeleteNoteAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    // --- Transaction ---

    [Test]
    public async Task DeleteNote_BeginsTransaction_BeforeWriting()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        NoteExists();

        await _controller.DeleteNote("proj-1", "note-1", CancellationToken.None);

        await _dbContext.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteNote_CommitsTransaction_AfterWriting()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        NoteExists();

        await _controller.DeleteNote("proj-1", "note-1", CancellationToken.None);

        await _dbContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // --- Note log ---

    [Test]
    public async Task DeleteNote_WritesNoteLog_WhenNoteIsDeleted()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        NoteExists("My Note", isFolder: false);

        await _controller.DeleteNote("proj-xyz", "note-abc", CancellationToken.None);

        await _repository.Received(1).InsertNoteLogAsync(_dbContext, "note-abc", "user-id-xyz", "proj-xyz", "My Note", false);
    }

    [Test]
    public async Task DeleteNote_WritesNoteLog_WhenFolderIsDeleted()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        NoteExists("My Folder", isFolder: true);

        await _controller.DeleteNote("proj-xyz", "folder-abc", CancellationToken.None);

        await _repository.Received(1).InsertNoteLogAsync(_dbContext, "folder-abc", "user-id-xyz", "proj-xyz", "My Folder", true);
    }

    [Test]
    public async Task DeleteNote_DoesNotWriteNoteLog_WhenAccessIsDenied()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.DeleteNote("proj-xyz", "note-1", CancellationToken.None);

        await _repository.DidNotReceive().InsertNoteLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    // --- Activity log ---

    [Test]
    public async Task DeleteNote_WritesActivityLog_WithNoteTitle()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        NoteExists("Meeting Notes", isFolder: false);

        await _controller.DeleteNote("proj-xyz", "note-abc", CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "note-abc", "user-id-xyz", "Note 'Meeting Notes' deleted");
    }

    [Test]
    public async Task DeleteNote_WritesActivityLog_WithFolderTitle()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        NoteExists("Project Docs", isFolder: true);

        await _controller.DeleteNote("proj-xyz", "folder-abc", CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "folder-abc", "user-id-xyz", "Folder 'Project Docs' deleted");
    }

    [Test]
    public async Task DeleteNote_DoesNotWriteActivityLog_WhenAccessIsDenied()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.DeleteNote("proj-xyz", "note-1", CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Event publishing ---

    [Test]
    public async Task DeleteNote_PublishesNoteDeletedEvent_WithCorrectNoteId()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        NoteExists();

        await _controller.DeleteNote("proj-1", "note-abc", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            NoteDeletedEvent.TopicId,
            Arg.Is<NoteDeletedEvent>(e => e.NoteId == "note-abc"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteNote_PublishesNoteDeletedEvent_WithCorrectProjectIdAndUserId()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        NoteExists();

        await _controller.DeleteNote("proj-xyz", "note-1", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            NoteDeletedEvent.TopicId,
            Arg.Is<NoteDeletedEvent>(e => e.ProjectId == "proj-xyz" && e.UserId == "user-id-xyz"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteNote_PublishesNoteDeletedEvent_WithCorrectTitleAndIsFolder()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        NoteExists("My Folder", isFolder: true);

        await _controller.DeleteNote("proj-1", "note-1", CancellationToken.None);

        await _eventPublisher.Received(1).PublishAsync(
            NoteDeletedEvent.TopicId,
            Arg.Is<NoteDeletedEvent>(e => e.Title == "My Folder" && e.IsFolder == true),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteNote_DoesNotPublishEvent_WhenNoteDoesNotExist()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        _repository.GetNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<NoteInfo?>(null));

        await _controller.DeleteNote("proj-1", "note-1", CancellationToken.None);

        await _eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<NoteDeletedEvent>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteNote_DoesNotPublishEvent_WhenAccessIsDenied()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.DeleteNote("proj-1", "note-1", CancellationToken.None);

        await _eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<NoteDeletedEvent>(), Arg.Any<CancellationToken>());
    }
}
