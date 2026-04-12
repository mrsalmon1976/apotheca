using System.Security.Claims;
using Apotheca.Api.Features.Projects.GetProjectOverview;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Projects.GetProjectOverview;

[TestFixture]
public class GetProjectOverviewControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private GetProjectOverviewRepository _repository = null!;
    private GetProjectOverviewController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<GetProjectOverviewRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _repository.GetOpenTaskCountAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(0));
        _repository.GetNoteCountAsync(_dbContext, Arg.Any<string>()).Returns(Task.FromResult(0));
        _repository.GetDocumentCountAsync(_dbContext, Arg.Any<string>()).Returns(Task.FromResult(0));

        _controller = new GetProjectOverviewController(_dbContextFactory, _repository);
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

    private void AllowProjectAccess()
    {
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
    }

    // --- Identity ---

    [Test]
    public async Task GetProjectOverview_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.GetProjectOverview("proj-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetProjectOverview_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.GetProjectOverview("proj-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task GetProjectOverview_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.GetProjectOverview("proj-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetProjectOverview_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.GetProjectOverview("proj-xyz", CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    // --- Result shape ---

    [Test]
    public async Task GetProjectOverview_ReturnsOk_WhenUserHasAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var result = await _controller.GetProjectOverview("proj-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetProjectOverview_ReturnsOpenTaskCount()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        _repository.GetOpenTaskCountAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(7));

        var result  = (OkObjectResult)await _controller.GetProjectOverview("proj-1", CancellationToken.None);
        var response = result.Value as GetProjectOverviewResponse;

        Assert.That(response!.OpenTaskCount, Is.EqualTo(7));
    }

    [Test]
    public async Task GetProjectOverview_ReturnsNoteCount()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        _repository.GetNoteCountAsync(_dbContext, Arg.Any<string>()).Returns(Task.FromResult(12));

        var result   = (OkObjectResult)await _controller.GetProjectOverview("proj-1", CancellationToken.None);
        var response = result.Value as GetProjectOverviewResponse;

        Assert.That(response!.NoteCount, Is.EqualTo(12));
    }

    [Test]
    public async Task GetProjectOverview_ReturnsDocumentCount()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        _repository.GetDocumentCountAsync(_dbContext, Arg.Any<string>()).Returns(Task.FromResult(3));

        var result   = (OkObjectResult)await _controller.GetProjectOverview("proj-1", CancellationToken.None);
        var response = result.Value as GetProjectOverviewResponse;

        Assert.That(response!.DocumentCount, Is.EqualTo(3));
    }

    [Test]
    public async Task GetProjectOverview_QueriesOpenTasksWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();

        await _controller.GetProjectOverview("proj-xyz", CancellationToken.None);

        await _repository.Received(1).GetOpenTaskCountAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    [Test]
    public async Task GetProjectOverview_QueriesNotesWithCorrectProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();

        await _controller.GetProjectOverview("proj-xyz", CancellationToken.None);

        await _repository.Received(1).GetNoteCountAsync(_dbContext, "proj-xyz");
    }

    [Test]
    public async Task GetProjectOverview_QueriesDocumentsWithCorrectProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();

        await _controller.GetProjectOverview("proj-xyz", CancellationToken.None);

        await _repository.Received(1).GetDocumentCountAsync(_dbContext, "proj-xyz");
    }
}
