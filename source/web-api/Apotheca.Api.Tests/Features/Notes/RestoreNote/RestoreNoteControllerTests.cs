using System.Security.Claims;
using Apotheca.Api.Features.Notes.RestoreNote;
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
    private RestoreNoteController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<RestoreNoteRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new RestoreNoteController(_dbContextFactory, _repository, Substitute.For<ILogger<RestoreNoteController>>());
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
        _repository.GetDeletedNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<NoteInfo?>(new NoteInfo(title, isFolder)));
    }

    // --- Identity ---

    [Test]
    public async Task RestoreNote_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task RestoreNote_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task RestoreNote_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task RestoreNote_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.RestoreNote("proj-xyz", "note-1", CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    // --- User lookup ---

    [Test]
    public async Task RestoreNote_Returns401_WhenUserIdCannotBeResolved()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        _repository.GetUserIdAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<string?>(null));

        var result = await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Note lookup ---

    [Test]
    public async Task RestoreNote_Returns404_WhenNoteDoesNotExist()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetDeletedNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<NoteInfo?>(null));

        var result = await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task RestoreNote_FetchesNoteInfoWithCorrectProjectIdAndNoteId()
    {
        SetAuthenticatedUser("firebase-uid");
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
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        NoteExists();

        var result = await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task RestoreNote_CallsRestore_WithCorrectNoteId()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        NoteExists();

        await _controller.RestoreNote("proj-1", "note-abc", CancellationToken.None);

        await _repository.Received(1).RestoreNoteAsync(_dbContext, "note-abc");
    }

    [Test]
    public async Task RestoreNote_DoesNotRestore_WhenAccessIsDenied()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        await _repository.DidNotReceive().RestoreNoteAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    [Test]
    public async Task RestoreNote_DoesNotRestore_WhenNoteDoesNotExist()
    {
        SetAuthenticatedUser("uid-abc");
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
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        NoteExists();

        await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        await _dbContext.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestoreNote_CommitsTransaction_AfterWriting()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        NoteExists();

        await _controller.RestoreNote("proj-1", "note-1", CancellationToken.None);

        await _dbContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // --- Note log ---

    [Test]
    public async Task RestoreNote_WritesNoteLog_WhenNoteIsRestored()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        NoteExists("My Note", isFolder: false);

        await _controller.RestoreNote("proj-xyz", "note-abc", CancellationToken.None);

        await _repository.Received(1).InsertNoteLogAsync(_dbContext, "note-abc", "user-id-xyz", "proj-xyz", "My Note", false);
    }

    [Test]
    public async Task RestoreNote_WritesNoteLog_WhenFolderIsRestored()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        NoteExists("My Folder", isFolder: true);

        await _controller.RestoreNote("proj-xyz", "folder-abc", CancellationToken.None);

        await _repository.Received(1).InsertNoteLogAsync(_dbContext, "folder-abc", "user-id-xyz", "proj-xyz", "My Folder", true);
    }

    [Test]
    public async Task RestoreNote_DoesNotWriteNoteLog_WhenAccessIsDenied()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.RestoreNote("proj-xyz", "note-1", CancellationToken.None);

        await _repository.DidNotReceive().InsertNoteLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    // --- Activity log ---

    [Test]
    public async Task RestoreNote_WritesActivityLog_WithNoteTitle()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        NoteExists("Meeting Notes", isFolder: false);

        await _controller.RestoreNote("proj-xyz", "note-abc", CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "note-abc", "user-id-xyz", "Note 'Meeting Notes' restored");
    }

    [Test]
    public async Task RestoreNote_WritesActivityLog_WithFolderTitle()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        NoteExists("Project Docs", isFolder: true);

        await _controller.RestoreNote("proj-xyz", "folder-abc", CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "folder-abc", "user-id-xyz", "Folder 'Project Docs' restored");
    }

    [Test]
    public async Task RestoreNote_DoesNotWriteActivityLog_WhenAccessIsDenied()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.RestoreNote("proj-xyz", "note-1", CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>());
    }
}
