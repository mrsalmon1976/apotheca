using System.Security.Claims;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Apotheca.Api.Tests.Providers;

[TestFixture]
public class SecurityProviderTests
{
    private IHttpContextAccessor _httpContextAccessor = null!;
    private IDbContext _dbContext = null!;
    private SecurityProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _dbContext            = Substitute.For<IDbContext>();
        _provider             = new SecurityProvider(_httpContextAccessor);

        _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext());
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private void SetFirebaseUid(string uid)
    {
        var claims   = new[] { new Claim("sub", uid) };
        var identity = new ClaimsIdentity(claims, "test");
        var ctx      = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        _httpContextAccessor.HttpContext.Returns(ctx);
    }

    private void SetupUserFound(string userId = "user-id-123")
    {
        _dbContext.QueryFirstOrDefaultAsync<string?>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(userId));
    }

    private void SetupUserNotFound()
    {
        _dbContext.QueryFirstOrDefaultAsync<string?>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));
    }

    // First call resolves the internal user id (AuthorizeAccessAsync); second call resolves the project/workspace role.
    private void SetupUserThenRole(string userId, string? role)
    {
        _dbContext.QueryFirstOrDefaultAsync<string?>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(userId), Task.FromResult<string?>(role));
    }

    // =========================================================================
    // AuthorizeAccessAsync
    // =========================================================================

    [Test]
    public async Task AuthorizeAccess_ReturnsFailure_WhenSubClaimIsMissing()
    {
        var result = await _provider.AuthorizeAccessAsync(_dbContext);

        Assert.That(result.IsAuthorized, Is.False);
    }

    [Test]
    public async Task AuthorizeAccess_ReturnsFailureWithMessage_WhenSubClaimIsMissing()
    {
        var result = await _provider.AuthorizeAccessAsync(_dbContext);

        Assert.That(result.ErrorMessage, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task AuthorizeAccess_ReturnsFailure_WhenUserNotFoundInDatabase()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserNotFound();

        var result = await _provider.AuthorizeAccessAsync(_dbContext);

        Assert.That(result.IsAuthorized, Is.False);
    }

    [Test]
    public async Task AuthorizeAccess_ReturnsFailureWithMessage_WhenUserNotFoundInDatabase()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserNotFound();

        var result = await _provider.AuthorizeAccessAsync(_dbContext);

        Assert.That(result.ErrorMessage, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task AuthorizeAccess_ReturnsSuccess_WhenUserIsFound()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserFound("user-id-123");

        var result = await _provider.AuthorizeAccessAsync(_dbContext);

        Assert.That(result.IsAuthorized, Is.True);
    }

    [Test]
    public async Task AuthorizeAccess_ReturnsCorrectFirebaseUid()
    {
        SetFirebaseUid("firebase-uid-abc");
        SetupUserFound();

        var result = await _provider.AuthorizeAccessAsync(_dbContext);

        Assert.That(result.FirebaseUid, Is.EqualTo("firebase-uid-abc"));
    }

    [Test]
    public async Task AuthorizeAccess_ReturnsCorrectUserId()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserFound("user-id-xyz");

        var result = await _provider.AuthorizeAccessAsync(_dbContext);

        Assert.That(result.UserId, Is.EqualTo("user-id-xyz"));
    }

    // =========================================================================
    // AuthorizeProjectAccessAsync
    // =========================================================================

    [Test]
    public async Task AuthorizeProjectAccess_ReturnsFailure_WhenSubClaimIsMissing()
    {
        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        Assert.That(result.IsAuthorized, Is.False);
    }

    [Test]
    public async Task AuthorizeProjectAccess_ReturnsFailure_WhenUserNotFoundInDatabase()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserNotFound();

        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        Assert.That(result.IsAuthorized, Is.False);
    }

    [Test]
    public async Task AuthorizeProjectAccess_ReturnsFailure_WhenProjectAccessDenied()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserThenRole("user-id-123", null);

        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        Assert.That(result.IsAuthorized, Is.False);
    }

    [Test]
    public async Task AuthorizeProjectAccess_ReturnsFailureWithMessage_WhenProjectAccessDenied()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserThenRole("user-id-123", null);

        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        Assert.That(result.ErrorMessage, Is.EqualTo("User does not have access to this project."));
    }

    [Test]
    public async Task AuthorizeProjectAccess_ReturnsSuccess_WhenAllChecksPass()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserThenRole("user-id-123", DataConstants.ProjectRole.Contributor);

        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        Assert.That(result.IsAuthorized, Is.True);
    }

    [Test]
    public async Task AuthorizeProjectAccess_ReturnsCorrectFirebaseUid()
    {
        SetFirebaseUid("firebase-uid-abc");
        SetupUserThenRole("user-id-123", DataConstants.ProjectRole.Contributor);

        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        Assert.That(result.FirebaseUid, Is.EqualTo("firebase-uid-abc"));
    }

    [Test]
    public async Task AuthorizeProjectAccess_ReturnsCorrectUserId()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserThenRole("user-id-xyz", DataConstants.ProjectRole.Contributor);

        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        Assert.That(result.UserId, Is.EqualTo("user-id-xyz"));
    }

    [Test]
    public async Task AuthorizeProjectAccess_ReturnsCorrectRole()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserThenRole("user-id-123", DataConstants.ProjectRole.Admin);

        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        Assert.That(result.Role, Is.EqualTo(DataConstants.ProjectRole.Admin));
    }

    [Test]
    public async Task AuthorizeProjectAccess_DoesNotCheckProjectAccess_WhenIdentityFails()
    {
        // No sub claim — identity check fails before project query runs
        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        await _dbContext.DidNotReceive().QueryFirstOrDefaultAsync<string?>(
            Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // AuthorizeWorkspaceAccessAsync
    // =========================================================================

    [Test]
    public async Task AuthorizeWorkspaceAccess_ReturnsFailure_WhenSubClaimIsMissing()
    {
        var result = await _provider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1");

        Assert.That(result.IsAuthorized, Is.False);
    }

    [Test]
    public async Task AuthorizeWorkspaceAccess_ReturnsFailure_WhenUserNotFoundInDatabase()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserNotFound();

        var result = await _provider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1");

        Assert.That(result.IsAuthorized, Is.False);
    }

    [Test]
    public async Task AuthorizeWorkspaceAccess_ReturnsFailure_WhenNotAMember()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserThenRole("user-id-123", null);

        var result = await _provider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1");

        Assert.That(result.IsAuthorized, Is.False);
    }

    [Test]
    public async Task AuthorizeWorkspaceAccess_ReturnsSuccess_WhenMember()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserThenRole("user-id-123", DataConstants.WorkspaceRole.Viewer);

        var result = await _provider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1");

        Assert.That(result.IsAuthorized, Is.True);
    }

    [Test]
    public async Task AuthorizeWorkspaceAccess_ReturnsCorrectRole()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserThenRole("user-id-123", DataConstants.WorkspaceRole.Admin);

        var result = await _provider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1");

        Assert.That(result.Role, Is.EqualTo(DataConstants.WorkspaceRole.Admin));
    }

    [Test]
    public async Task AuthorizeWorkspaceAccess_ReturnsSuccess_WhenViewerAndAdminNotRequired()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserThenRole("user-id-123", DataConstants.WorkspaceRole.Viewer);

        var result = await _provider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", requireAdmin: false);

        Assert.That(result.IsAuthorized, Is.True);
    }

    [Test]
    public async Task AuthorizeWorkspaceAccess_ReturnsFailure_WhenViewerButAdminRequired()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserThenRole("user-id-123", DataConstants.WorkspaceRole.Viewer);

        var result = await _provider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", requireAdmin: true);

        Assert.That(result.IsAuthorized, Is.False);
    }

    [Test]
    public async Task AuthorizeWorkspaceAccess_ReturnsSuccess_WhenAdminAndAdminRequired()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserThenRole("user-id-123", DataConstants.WorkspaceRole.Admin);

        var result = await _provider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", requireAdmin: true);

        Assert.That(result.IsAuthorized, Is.True);
    }
}
