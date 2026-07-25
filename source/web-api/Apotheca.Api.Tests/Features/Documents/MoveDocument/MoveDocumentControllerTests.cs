using Apotheca.Api.Features.Documents.MoveDocument;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.MoveDocument;

[TestFixture]
public class MoveDocumentControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private MoveDocumentRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private MoveDocumentController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<MoveDocumentRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _repository.TargetFolderExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(true));
        _repository.WouldCreateCycleAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(false));
        _repository.GetFolderTitleAsync(_dbContext, Arg.Any<string>()).Returns(Task.FromResult<string?>("Target Folder"));

        _controller = new MoveDocumentController(_dbContextFactory, _repository, _securityProvider, Substitute.For<ILogger<MoveDocumentController>>());
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

    private void DocumentExists(string title = "My Document", bool isFolder = false, string? parentDocumentId = null)
    {
        _repository.GetDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<MoveDocumentInfo?>(new MoveDocumentInfo(title, isFolder, parentDocumentId)));
    }

    private static MoveDocumentRequest ToTarget(string? targetFolderId) => new() { TargetFolderId = targetFolderId };

    // --- Identity / Access control ---

    [Test]
    public async Task MoveDocument_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.MoveDocument("proj-1", "doc-1", ToTarget("folder-1"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task MoveDocument_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.MoveDocument("proj-1", "doc-1", ToTarget("folder-1"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Document lookup ---

    [Test]
    public async Task MoveDocument_Returns404_WhenDocumentDoesNotExist()
    {
        AllowProjectAccess();
        _repository.GetDocumentInfoAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<MoveDocumentInfo?>(null));

        var result = await _controller.MoveDocument("proj-1", "doc-1", ToTarget("folder-1"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // --- Self-move ---

    [Test]
    public async Task MoveDocument_Returns400_WhenTargetIsTheItemItself()
    {
        AllowProjectAccess();
        DocumentExists();

        var result = await _controller.MoveDocument("proj-1", "doc-1", ToTarget("doc-1"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task MoveDocument_DoesNotMove_WhenTargetIsTheItemItself()
    {
        AllowProjectAccess();
        DocumentExists();

        await _controller.MoveDocument("proj-1", "doc-1", ToTarget("doc-1"), CancellationToken.None);

        await _repository.DidNotReceive().MoveDocumentAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>());
    }

    // --- No-op when already in target ---

    [Test]
    public async Task MoveDocument_Returns200_WhenAlreadyInTargetFolder()
    {
        AllowProjectAccess();
        DocumentExists(parentDocumentId: "folder-1");

        var result = await _controller.MoveDocument("proj-1", "doc-1", ToTarget("folder-1"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task MoveDocument_DoesNotMove_WhenAlreadyInTargetFolder()
    {
        AllowProjectAccess();
        DocumentExists(parentDocumentId: "folder-1");

        await _controller.MoveDocument("proj-1", "doc-1", ToTarget("folder-1"), CancellationToken.None);

        await _repository.DidNotReceive().MoveDocumentAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>());
    }

    [Test]
    public async Task MoveDocument_Returns200_WhenAlreadyAtRoot_AndTargetIsRoot()
    {
        AllowProjectAccess();
        DocumentExists(parentDocumentId: null);

        var result = await _controller.MoveDocument("proj-1", "doc-1", ToTarget(null), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    // --- Target folder lookup ---

    [Test]
    public async Task MoveDocument_Returns404_WhenTargetFolderDoesNotExist()
    {
        AllowProjectAccess();
        DocumentExists(parentDocumentId: "other-folder");
        _repository.TargetFolderExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(false));

        var result = await _controller.MoveDocument("proj-1", "doc-1", ToTarget("missing-folder"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task MoveDocument_DoesNotCheckTargetFolderExistence_WhenMovingToRoot()
    {
        AllowProjectAccess();
        DocumentExists(parentDocumentId: "other-folder");

        await _controller.MoveDocument("proj-1", "doc-1", ToTarget(null), CancellationToken.None);

        await _repository.DidNotReceive().TargetFolderExistsAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Cycle prevention ---

    [Test]
    public async Task MoveDocument_Returns400_WhenMovingFolderIntoOwnDescendant()
    {
        AllowProjectAccess();
        DocumentExists(isFolder: true, parentDocumentId: "other-folder");
        _repository.WouldCreateCycleAsync(_dbContext, "doc-1", "descendant-folder").Returns(Task.FromResult(true));

        var result = await _controller.MoveDocument("proj-1", "doc-1", ToTarget("descendant-folder"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task MoveDocument_DoesNotCheckForCycle_WhenItemIsNotAFolder()
    {
        AllowProjectAccess();
        DocumentExists(isFolder: false, parentDocumentId: "other-folder");

        await _controller.MoveDocument("proj-1", "doc-1", ToTarget("folder-2"), CancellationToken.None);

        await _repository.DidNotReceive().WouldCreateCycleAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Success ---

    [Test]
    public async Task MoveDocument_Returns200_WhenMoveSucceeds()
    {
        AllowProjectAccess();
        DocumentExists(parentDocumentId: "other-folder");

        var result = await _controller.MoveDocument("proj-1", "doc-1", ToTarget("folder-2"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task MoveDocument_CallsMove_WithCorrectArguments()
    {
        AllowProjectAccess();
        DocumentExists(parentDocumentId: "other-folder");

        await _controller.MoveDocument("proj-xyz", "doc-abc", ToTarget("folder-2"), CancellationToken.None);

        await _repository.Received(1).MoveDocumentAsync(_dbContext, "proj-xyz", "doc-abc", "folder-2");
    }

    [Test]
    public async Task MoveDocument_TreatsBlankTargetFolderId_AsRoot()
    {
        AllowProjectAccess();
        DocumentExists(parentDocumentId: "other-folder");

        await _controller.MoveDocument("proj-1", "doc-1", ToTarget("   "), CancellationToken.None);

        await _repository.Received(1).MoveDocumentAsync(_dbContext, "proj-1", "doc-1", null);
    }

    // --- Transaction ---

    [Test]
    public async Task MoveDocument_BeginsTransaction_BeforeWriting()
    {
        AllowProjectAccess();
        DocumentExists(parentDocumentId: "other-folder");

        await _controller.MoveDocument("proj-1", "doc-1", ToTarget("folder-2"), CancellationToken.None);

        await _dbContext.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MoveDocument_CommitsTransaction_AfterWriting()
    {
        AllowProjectAccess();
        DocumentExists(parentDocumentId: "other-folder");

        await _controller.MoveDocument("proj-1", "doc-1", ToTarget("folder-2"), CancellationToken.None);

        await _dbContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // --- Activity log ---

    [Test]
    public async Task MoveDocument_WritesActivityLog_WithDocumentMovedToFolder()
    {
        AllowProjectAccess("user-id-xyz");
        DocumentExists("Invoice.pdf", isFolder: false, parentDocumentId: "other-folder");
        _repository.GetFolderTitleAsync(_dbContext, "folder-2").Returns(Task.FromResult<string?>("Archive"));

        await _controller.MoveDocument("proj-xyz", "doc-abc", ToTarget("folder-2"), CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "doc-abc", "user-id-xyz", "Document 'Invoice.pdf' moved to 'Archive'");
    }

    [Test]
    public async Task MoveDocument_WritesActivityLog_WithFolderMovedToRoot()
    {
        AllowProjectAccess("user-id-xyz");
        DocumentExists("Contracts", isFolder: true, parentDocumentId: "other-folder");

        await _controller.MoveDocument("proj-xyz", "folder-abc", ToTarget(null), CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "folder-abc", "user-id-xyz", "Folder 'Contracts' moved to root");
    }

    [Test]
    public async Task MoveDocument_DoesNotWriteActivityLog_WhenAlreadyInTargetFolder()
    {
        AllowProjectAccess();
        DocumentExists(parentDocumentId: "folder-1");

        await _controller.MoveDocument("proj-1", "doc-1", ToTarget("folder-1"), CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task MoveDocument_DoesNotWriteActivityLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.MoveDocument("proj-1", "doc-1", ToTarget("folder-2"), CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
