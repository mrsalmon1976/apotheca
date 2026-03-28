using System.Security.Claims;
using Apotheca.Api.Features.Notes.SaveNoteFolder;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Notes.SaveNoteFolder;

[TestFixture]
public class SaveNoteFolderControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private SaveNoteFolderRepository _repository = null!;
    private SaveNoteFolderValidator _validator = null!;
    private SaveNoteFolderController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repository = Substitute.For<SaveNoteFolderRepository>();
        _validator = Substitute.For<SaveNoteFolderValidator>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _validator.Validate(Arg.Any<SaveNoteFolderRequest>()).Returns([]);

        _controller = new SaveNoteFolderController(_dbContextFactory, _repository, _validator);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    private void SetAuthenticatedUser(string firebaseUid)
    {
        var claims = new[] { new Claim("sub", firebaseUid) };
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

    private static SaveNoteFolderRequest ValidRequest() => new()
    {
        Title = "Meeting Notes",
    };

    // --- Validation ---

    [Test]
    public async Task SaveNoteFolder_Returns400_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveNoteFolderRequest>()).Returns(["Folder name is required."]);

        var result = await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SaveNoteFolder_ReturnsValidationErrors_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveNoteFolderRequest>()).Returns(["Folder name is required."]);

        var result = (BadRequestObjectResult)await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);
        var errors = result.Value?.GetType().GetProperty("errors")?.GetValue(result.Value) as IReadOnlyList<string>;

        Assert.That(errors, Has.One.EqualTo("Folder name is required."));
    }

    [Test]
    public async Task SaveNoteFolder_DoesNotOpenDatabase_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveNoteFolderRequest>()).Returns(["Folder name is required."]);

        await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);

        await _dbContextFactory.DidNotReceive().CreateAsync(Arg.Any<CancellationToken>());
    }

    // --- Identity ---

    [Test]
    public async Task SaveNoteFolder_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SaveNoteFolder_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);
        var error = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task SaveNoteFolder_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task SaveNoteFolder_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.SaveNoteFolder("proj-xyz", ValidRequest(), CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    // --- User lookup ---

    [Test]
    public async Task SaveNoteFolder_Returns401_WhenUserIdCannotBeResolved()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        _repository.GetUserIdAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<string?>(null));

        var result = await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Success ---

    [Test]
    public async Task SaveNoteFolder_Returns201_WhenFolderIsCreated()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-folder-id"));

        var result = await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task SaveNoteFolder_ReturnsNewId_WhenFolderIsCreated()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-folder-id"));

        var result = (CreatedAtActionResult)await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);
        var id = result.Value?.GetType().GetProperty("id")?.GetValue(result.Value)?.ToString();

        Assert.That(id, Is.EqualTo("new-folder-id"));
    }

    [Test]
    public async Task SaveNoteFolder_CallsInsert_WithCorrectProjectIdAndUserId()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        _repository.InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        await _controller.SaveNoteFolder("proj-xyz", ValidRequest(), CancellationToken.None);

        await _repository.Received(1).InsertNoteFolderAsync(_dbContext, "proj-xyz", "user-id-xyz", Arg.Any<string>(), Arg.Any<string?>());
    }

    [Test]
    public async Task SaveNoteFolder_TrimsTitle_BeforeCallingRepository()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        _repository.InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        var request = new SaveNoteFolderRequest { Title = "  My Folder  " };
        await _controller.SaveNoteFolder("proj-1", request, CancellationToken.None);

        await _repository.Received(1).InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), "My Folder", Arg.Any<string?>());
    }

    [Test]
    public async Task SaveNoteFolder_ForwardsParentNoteId_ToRepository()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        _repository.InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        var request = new SaveNoteFolderRequest { Title = "Sub Folder", ParentNoteId = "parent-folder-id" };
        await _controller.SaveNoteFolder("proj-1", request, CancellationToken.None);

        await _repository.Received(1).InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), "parent-folder-id");
    }
}
