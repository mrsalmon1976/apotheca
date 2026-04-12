using System.Security.Claims;
using Apotheca.Api.Features.Notes.CreateNote;
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
    private CreateNoteController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<CreateNoteRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new CreateNoteController(_dbContextFactory, _repository, Substitute.For<ILogger<CreateNoteController>>());
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

    // --- Identity ---

    [Test]
    public async Task CreateNote_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.CreateNote("proj-1", new CreateNoteRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task CreateNote_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.CreateNote("proj-1", new CreateNoteRequest(), CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task CreateNote_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.CreateNote("proj-1", new CreateNoteRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task CreateNote_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.CreateNote("proj-xyz", new CreateNoteRequest(), CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    // --- User lookup ---

    [Test]
    public async Task CreateNote_Returns401_WhenUserIdCannotBeResolved()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        _repository.GetUserIdAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<string?>(null));

        var result = await _controller.CreateNote("proj-1", new CreateNoteRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Success ---

    [Test]
    public async Task CreateNote_Returns201_WhenNoteIsCreated()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.InsertNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-note-id"));

        var result = await _controller.CreateNote("proj-1", new CreateNoteRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task CreateNote_ReturnsNewId_WhenNoteIsCreated()
    {
        SetAuthenticatedUser("firebase-uid");
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
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        _repository.InsertNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        await _controller.CreateNote("proj-xyz", new CreateNoteRequest(), CancellationToken.None);

        await _repository.Received(1).InsertNoteAsync(_dbContext, "proj-xyz", "user-id-xyz", Arg.Any<string?>());
    }

    [Test]
    public async Task CreateNote_ForwardsParentNoteId_ToRepository()
    {
        SetAuthenticatedUser("uid-abc");
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
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        _repository.InsertNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        await _controller.CreateNote("proj-1", new CreateNoteRequest(), CancellationToken.None);

        await _repository.Received(1).InsertNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Is<string?>(x => x == null));
    }
}
