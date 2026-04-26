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

    private void SetupProjectAccessGranted()
    {
        _dbContext.QueryFirstOrDefaultAsync<int>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));
    }

    private void SetupProjectAccessDenied()
    {
        _dbContext.QueryFirstOrDefaultAsync<int>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));
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
        SetupUserFound();
        SetupProjectAccessDenied();

        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        Assert.That(result.IsAuthorized, Is.False);
    }

    [Test]
    public async Task AuthorizeProjectAccess_ReturnsFailureWithMessage_WhenProjectAccessDenied()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserFound();
        SetupProjectAccessDenied();

        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        Assert.That(result.ErrorMessage, Is.EqualTo("User does not have access to this project."));
    }

    [Test]
    public async Task AuthorizeProjectAccess_ReturnsSuccess_WhenAllChecksPass()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserFound();
        SetupProjectAccessGranted();

        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        Assert.That(result.IsAuthorized, Is.True);
    }

    [Test]
    public async Task AuthorizeProjectAccess_ReturnsCorrectFirebaseUid()
    {
        SetFirebaseUid("firebase-uid-abc");
        SetupUserFound();
        SetupProjectAccessGranted();

        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        Assert.That(result.FirebaseUid, Is.EqualTo("firebase-uid-abc"));
    }

    [Test]
    public async Task AuthorizeProjectAccess_ReturnsCorrectUserId()
    {
        SetFirebaseUid("firebase-uid");
        SetupUserFound("user-id-xyz");
        SetupProjectAccessGranted();

        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        Assert.That(result.UserId, Is.EqualTo("user-id-xyz"));
    }

    [Test]
    public async Task AuthorizeProjectAccess_DoesNotCheckProjectAccess_WhenIdentityFails()
    {
        // No sub claim — identity check fails before project query runs
        var result = await _provider.AuthorizeProjectAccessAsync(_dbContext, "proj-1");

        await _dbContext.DidNotReceive().QueryFirstOrDefaultAsync<int>(
            Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }
}
