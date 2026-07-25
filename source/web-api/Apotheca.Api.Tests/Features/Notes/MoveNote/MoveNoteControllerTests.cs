using Apotheca.Api.Features.Notes.MoveNote;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Notes.MoveNote;

[TestFixture]
public class MoveNoteControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private MoveNoteRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private MoveNoteController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<MoveNoteRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _repository.TargetFolderExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(true));
        _repository.WouldCreateCycleAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(false));
        _repository.GetFolderTitleAsync(_dbContext, Arg.Any<string>()).Returns(Task.FromResult<string?>("Target Folder"));

        _controller = new MoveNoteController(_dbContextFactory, _repository, _securityProvider, Substitute.For<ILogger<MoveNoteController>>());
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

    private void NoteExists(string title = "My Note", bool isFolder = false, string? parentNoteId = null)
    {
        _repository.GetNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<MoveNoteInfo?>(new MoveNoteInfo(title, isFolder, parentNoteId)));
    }

    private static MoveNoteRequest ToTarget(string? targetFolderId) => new() { TargetFolderId = targetFolderId };

    // --- Identity / Access control ---

    [Test]
    public async Task MoveNote_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.MoveNote("proj-1", "note-1", ToTarget("folder-1"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task MoveNote_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.MoveNote("proj-1", "note-1", ToTarget("folder-1"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Note lookup ---

    [Test]
    public async Task MoveNote_Returns404_WhenNoteDoesNotExist()
    {
        AllowProjectAccess();
        _repository.GetNoteInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<MoveNoteInfo?>(null));

        var result = await _controller.MoveNote("proj-1", "note-1", ToTarget("folder-1"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // --- Self-move ---

    [Test]
    public async Task MoveNote_Returns400_WhenTargetIsTheItemItself()
    {
        AllowProjectAccess();
        NoteExists();

        var result = await _controller.MoveNote("proj-1", "note-1", ToTarget("note-1"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task MoveNote_DoesNotMove_WhenTargetIsTheItemItself()
    {
        AllowProjectAccess();
        NoteExists();

        await _controller.MoveNote("proj-1", "note-1", ToTarget("note-1"), CancellationToken.None);

        await _repository.DidNotReceive().MoveNoteAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>());
    }

    // --- No-op when already in target ---

    [Test]
    public async Task MoveNote_Returns200_WhenAlreadyInTargetFolder()
    {
        AllowProjectAccess();
        NoteExists(parentNoteId: "folder-1");

        var result = await _controller.MoveNote("proj-1", "note-1", ToTarget("folder-1"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task MoveNote_DoesNotMove_WhenAlreadyInTargetFolder()
    {
        AllowProjectAccess();
        NoteExists(parentNoteId: "folder-1");

        await _controller.MoveNote("proj-1", "note-1", ToTarget("folder-1"), CancellationToken.None);

        await _repository.DidNotReceive().MoveNoteAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>());
    }

    [Test]
    public async Task MoveNote_Returns200_WhenAlreadyAtRoot_AndTargetIsRoot()
    {
        AllowProjectAccess();
        NoteExists(parentNoteId: null);

        var result = await _controller.MoveNote("proj-1", "note-1", ToTarget(null), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    // --- Target folder lookup ---

    [Test]
    public async Task MoveNote_Returns404_WhenTargetFolderDoesNotExist()
    {
        AllowProjectAccess();
        NoteExists(parentNoteId: "other-folder");
        _repository.TargetFolderExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(false));

        var result = await _controller.MoveNote("proj-1", "note-1", ToTarget("missing-folder"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task MoveNote_DoesNotCheckTargetFolderExistence_WhenMovingToRoot()
    {
        AllowProjectAccess();
        NoteExists(parentNoteId: "other-folder");

        await _controller.MoveNote("proj-1", "note-1", ToTarget(null), CancellationToken.None);

        await _repository.DidNotReceive().TargetFolderExistsAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Cycle prevention ---

    [Test]
    public async Task MoveNote_Returns400_WhenMovingFolderIntoOwnDescendant()
    {
        AllowProjectAccess();
        NoteExists(isFolder: true, parentNoteId: "other-folder");
        _repository.WouldCreateCycleAsync(_dbContext, "note-1", "descendant-folder").Returns(Task.FromResult(true));

        var result = await _controller.MoveNote("proj-1", "note-1", ToTarget("descendant-folder"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task MoveNote_DoesNotCheckForCycle_WhenItemIsNotAFolder()
    {
        AllowProjectAccess();
        NoteExists(isFolder: false, parentNoteId: "other-folder");

        await _controller.MoveNote("proj-1", "note-1", ToTarget("folder-2"), CancellationToken.None);

        await _repository.DidNotReceive().WouldCreateCycleAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Success ---

    [Test]
    public async Task MoveNote_Returns200_WhenMoveSucceeds()
    {
        AllowProjectAccess();
        NoteExists(parentNoteId: "other-folder");

        var result = await _controller.MoveNote("proj-1", "note-1", ToTarget("folder-2"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task MoveNote_CallsMove_WithCorrectArguments()
    {
        AllowProjectAccess();
        NoteExists(parentNoteId: "other-folder");

        await _controller.MoveNote("proj-xyz", "note-abc", ToTarget("folder-2"), CancellationToken.None);

        await _repository.Received(1).MoveNoteAsync(_dbContext, "proj-xyz", "note-abc", "folder-2");
    }

    [Test]
    public async Task MoveNote_TreatsBlankTargetFolderId_AsRoot()
    {
        AllowProjectAccess();
        NoteExists(parentNoteId: "other-folder");

        await _controller.MoveNote("proj-1", "note-1", ToTarget("   "), CancellationToken.None);

        await _repository.Received(1).MoveNoteAsync(_dbContext, "proj-1", "note-1", null);
    }

    // --- Transaction ---

    [Test]
    public async Task MoveNote_BeginsTransaction_BeforeWriting()
    {
        AllowProjectAccess();
        NoteExists(parentNoteId: "other-folder");

        await _controller.MoveNote("proj-1", "note-1", ToTarget("folder-2"), CancellationToken.None);

        await _dbContext.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MoveNote_CommitsTransaction_AfterWriting()
    {
        AllowProjectAccess();
        NoteExists(parentNoteId: "other-folder");

        await _controller.MoveNote("proj-1", "note-1", ToTarget("folder-2"), CancellationToken.None);

        await _dbContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // --- Activity log ---

    [Test]
    public async Task MoveNote_WritesActivityLog_WithNoteMovedToFolder()
    {
        AllowProjectAccess("user-id-xyz");
        NoteExists("Meeting Notes", isFolder: false, parentNoteId: "other-folder");
        _repository.GetFolderTitleAsync(_dbContext, "folder-2").Returns(Task.FromResult<string?>("Archive"));

        await _controller.MoveNote("proj-xyz", "note-abc", ToTarget("folder-2"), CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "note-abc", "user-id-xyz", "Note 'Meeting Notes' moved to 'Archive'");
    }

    [Test]
    public async Task MoveNote_WritesActivityLog_WithFolderMovedToRoot()
    {
        AllowProjectAccess("user-id-xyz");
        NoteExists("Project Docs", isFolder: true, parentNoteId: "other-folder");

        await _controller.MoveNote("proj-xyz", "folder-abc", ToTarget(null), CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "folder-abc", "user-id-xyz", "Folder 'Project Docs' moved to root");
    }

    [Test]
    public async Task MoveNote_DoesNotWriteActivityLog_WhenAlreadyInTargetFolder()
    {
        AllowProjectAccess();
        NoteExists(parentNoteId: "folder-1");

        await _controller.MoveNote("proj-1", "note-1", ToTarget("folder-1"), CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task MoveNote_DoesNotWriteActivityLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.MoveNote("proj-1", "note-1", ToTarget("folder-2"), CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
