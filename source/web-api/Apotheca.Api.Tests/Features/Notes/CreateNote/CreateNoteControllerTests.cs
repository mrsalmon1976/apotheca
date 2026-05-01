using Apotheca.Api.Features.Notes.CreateNote;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Notes.CreateNote;

[TestFixture]
public class CreateNoteControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private CreateNoteRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private CreateNoteController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<CreateNoteRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new CreateNoteController(_dbContextFactory, _repository, _securityProvider, Substitute.For<ILogger<CreateNoteController>>());
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

    // --- Identity / Access control ---

    [Test]
    public async Task CreateNote_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.CreateNote("proj-1", new CreateNoteRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task CreateNote_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.CreateNote("proj-1", new CreateNoteRequest(), CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task CreateNote_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.CreateNote("proj-1", new CreateNoteRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Success ---

    [Test]
    public async Task CreateNote_Returns201_WhenNoteIsCreated()
    {
        AllowProjectAccess();
        _repository.InsertNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-note-id"));

        var result = await _controller.CreateNote("proj-1", new CreateNoteRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task CreateNote_ReturnsNewId_WhenNoteIsCreated()
    {
        AllowProjectAccess();
        _repository.InsertNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-note-id"));

        var result = (CreatedAtActionResult)await _controller.CreateNote("proj-1", new CreateNoteRequest(), CancellationToken.None);
        var id     = result.Value?.GetType().GetProperty("id")?.GetValue(result.Value)?.ToString();

        Assert.That(id, Is.EqualTo("new-note-id"));
    }

    [Test]
    public async Task CreateNote_CallsInsert_WithCorrectProjectIdAndUserId()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.InsertNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        await _controller.CreateNote("proj-xyz", new CreateNoteRequest(), CancellationToken.None);

        await _repository.Received(1).InsertNoteAsync(_dbContext, "proj-xyz", "user-id-xyz", Arg.Any<string?>());
    }

    [Test]
    public async Task CreateNote_ForwardsParentNoteId_ToRepository()
    {
        AllowProjectAccess();
        _repository.InsertNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        var request = new CreateNoteRequest { ParentNoteId = "parent-folder-id" };
        await _controller.CreateNote("proj-1", request, CancellationToken.None);

        await _repository.Received(1).InsertNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), "parent-folder-id");
    }

    [Test]
    public async Task CreateNote_PassesNullParentNoteId_WhenNotProvided()
    {
        AllowProjectAccess();
        _repository.InsertNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        await _controller.CreateNote("proj-1", new CreateNoteRequest(), CancellationToken.None);

        await _repository.Received(1).InsertNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Is<string?>(x => x == null));
    }

    // --- Note log ---

    [Test]
    public async Task CreateNote_WritesNoteLog_WhenNoteIsCreated()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.InsertNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-note-id"));

        await _controller.CreateNote("proj-xyz", new CreateNoteRequest(), CancellationToken.None);

        await _repository.Received(1).InsertNoteLogAsync(_dbContext, "new-note-id", "user-id-xyz", "proj-xyz");
    }

    [Test]
    public async Task CreateNote_DoesNotWriteNoteLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.CreateNote("proj-xyz", new CreateNoteRequest(), CancellationToken.None);

        await _repository.DidNotReceive().InsertNoteLogAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Activity log ---

    [Test]
    public async Task CreateNote_WritesProjectActivityLog_WhenNoteIsCreated()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.InsertNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-note-id"));

        await _controller.CreateNote("proj-xyz", new CreateNoteRequest(), CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(_dbContext, "proj-xyz", "new-note-id", "user-id-xyz", "Note added");
    }

    [Test]
    public async Task CreateNote_DoesNotWriteProjectActivityLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.CreateNote("proj-xyz", new CreateNoteRequest(), CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Search ---

    [Test]
    public async Task CreateNote_UpsertsSearchRecord_WhenNoteIsCreated()
    {
        AllowProjectAccess();
        _repository.InsertNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-note-id"));

        await _controller.CreateNote("proj-1", new CreateNoteRequest(), CancellationToken.None);

        await _repository.Received(1).UpsertSearchAsync(_dbContext, "proj-1", "new-note-id", "New Note", "");
    }

    [Test]
    public async Task CreateNote_DoesNotUpsertSearchRecord_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.CreateNote("proj-1", new CreateNoteRequest(), CancellationToken.None);

        await _repository.DidNotReceive().UpsertSearchAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
