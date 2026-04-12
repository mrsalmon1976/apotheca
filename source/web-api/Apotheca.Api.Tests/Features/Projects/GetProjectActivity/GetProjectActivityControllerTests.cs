using System.Security.Claims;
using Apotheca.Api.Features.Projects.GetProjectActivity;
using Apotheca.Data;
using Apotheca.Data.DbEntities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Projects.GetProjectActivity;

[TestFixture]
public class GetProjectActivityControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private GetProjectActivityRepository _repository = null!;
    private GetProjectActivityController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repository = Substitute.For<GetProjectActivityRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _repository.GetProjectActivityAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(Enumerable.Empty<ProjectActivityLogDbEntity>()));

        _controller = new GetProjectActivityController(_dbContextFactory, _repository);
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

    private void AllowProjectAccess()
    {
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
    }

    // --- Identity ---

    [Test]
    public async Task GetProjectActivity_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.GetProjectActivity("proj-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetProjectActivity_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.GetProjectActivity("proj-1", CancellationToken.None);
        var error = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task GetProjectActivity_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.GetProjectActivity("proj-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetProjectActivity_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.GetProjectActivity("proj-xyz", CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    // --- Result shape ---

    [Test]
    public async Task GetProjectActivity_ReturnsOk_WhenUserHasAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var result = await _controller.GetProjectActivity("proj-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetProjectActivity_ReturnsEmptyList_WhenNoActivityExists()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var result = (OkObjectResult)await _controller.GetProjectActivity("proj-1", CancellationToken.None);
        var entries = result.Value as IEnumerable<GetProjectActivityResponse>;

        Assert.That(entries, Is.Empty);
    }

    [Test]
    public async Task GetProjectActivity_QueriesWithCorrectProjectId()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        await _controller.GetProjectActivity("proj-xyz", CancellationToken.None);

        await _repository.Received(1).GetProjectActivityAsync(_dbContext, "proj-xyz");
    }

    [Test]
    public async Task GetProjectActivity_ReturnsMappedEntries()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var now = DateTimeOffset.UtcNow;
        var dbResults = new[]
        {
            new ProjectActivityLogDbEntity { Id = 1, RefId = "ref-aaa", RefType = "NOTE",    LogMessage = "Note added",    Username = "Alice", CreatedAt = now },
            new ProjectActivityLogDbEntity { Id = 2, RefId = "ref-bbb", RefType = "PROJECT", LogMessage = "Project created", Username = "Bob",   CreatedAt = now },
        };
        _repository.GetProjectActivityAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<ProjectActivityLogDbEntity>>(dbResults));

        var result = (OkObjectResult)await _controller.GetProjectActivity("proj-1", CancellationToken.None);
        var entries = (result.Value as IEnumerable<GetProjectActivityResponse>)!.ToList();

        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.That(entries[0].RefId, Is.EqualTo("ref-aaa"));
        Assert.That(entries[0].RefType, Is.EqualTo("NOTE"));
        Assert.That(entries[0].Username, Is.EqualTo("Alice"));
        Assert.That(entries[1].RefId, Is.EqualTo("ref-bbb"));
        Assert.That(entries[1].Username, Is.EqualTo("Bob"));
    }
}
