using System.Security.Claims;
using Apotheca.Api.Features.Documents.CreateDocument;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.CreateDocument;

[TestFixture]
public class CreateDocumentControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private CreateDocumentRepository _repository = null!;
    private CreateDocumentController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<CreateDocumentRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new CreateDocumentController(_dbContextFactory, _repository, Substitute.For<ILogger<CreateDocumentController>>());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

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
    public async Task CreateDocument_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.CreateDocument("proj-1", new CreateDocumentRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task CreateDocument_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.CreateDocument("proj-1", new CreateDocumentRequest(), CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task CreateDocument_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.CreateDocument("proj-1", new CreateDocumentRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task CreateDocument_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.CreateDocument("proj-xyz", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    // --- User lookup ---

    [Test]
    public async Task CreateDocument_Returns401_WhenUserIdCannotBeResolved()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        _repository.GetUserIdAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<string?>(null));

        var result = await _controller.CreateDocument("proj-1", new CreateDocumentRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Success ---

    [Test]
    public async Task CreateDocument_Returns201_WhenDocumentIsCreated()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-doc-id"));

        var result = await _controller.CreateDocument("proj-1", new CreateDocumentRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task CreateDocument_ReturnsNewId_WhenDocumentIsCreated()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-doc-id"));

        var result = (CreatedAtActionResult)await _controller.CreateDocument("proj-1", new CreateDocumentRequest(), CancellationToken.None);
        var id     = result.Value?.GetType().GetProperty("id")?.GetValue(result.Value)?.ToString();

        Assert.That(id, Is.EqualTo("new-doc-id"));
    }

    [Test]
    public async Task CreateDocument_CallsInsert_WithCorrectProjectIdAndUserId()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        await _controller.CreateDocument("proj-xyz", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.Received(1).InsertDocumentAsync(_dbContext, "proj-xyz", "user-id-xyz", Arg.Any<string?>());
    }

    [Test]
    public async Task CreateDocument_ForwardsParentDocumentId_ToRepository()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        var request = new CreateDocumentRequest { ParentDocumentId = "parent-folder-id" };
        await _controller.CreateDocument("proj-1", request, CancellationToken.None);

        await _repository.Received(1).InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), "parent-folder-id");
    }

    [Test]
    public async Task CreateDocument_PassesNullParentDocumentId_WhenNotProvided()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        await _controller.CreateDocument("proj-1", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.Received(1).InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Is<string?>(x => x == null));
    }

    // --- Document log ---

    [Test]
    public async Task CreateDocument_WritesDocumentLog_WhenDocumentIsCreated()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-doc-id"));

        await _controller.CreateDocument("proj-xyz", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.Received(1).InsertDocumentLogAsync(_dbContext, "new-doc-id", "user-id-xyz", "proj-xyz");
    }

    [Test]
    public async Task CreateDocument_DoesNotWriteDocumentLog_WhenAccessIsDenied()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.CreateDocument("proj-xyz", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.DidNotReceive().InsertDocumentLogAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Activity log ---

    [Test]
    public async Task CreateDocument_WritesProjectActivityLog_WhenDocumentIsCreated()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-doc-id"));

        await _controller.CreateDocument("proj-xyz", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(_dbContext, "proj-xyz", "new-doc-id", "user-id-xyz", "Document added");
    }

    [Test]
    public async Task CreateDocument_DoesNotWriteActivityLog_WhenAccessIsDenied()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.CreateDocument("proj-xyz", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
