using Apotheca.Api.Features.Projects.GetProjectOverview;
using Apotheca.Api.Providers;
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
    private ISecurityProvider _securityProvider = null!;
    private GetProjectOverviewController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<GetProjectOverviewRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _repository.GetOpenTaskCountAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(0));
        _repository.GetNoteCountAsync(_dbContext, Arg.Any<string>()).Returns(Task.FromResult(0));
        _repository.GetDocumentCountAsync(_dbContext, Arg.Any<string>()).Returns(Task.FromResult(0));

        _controller = new GetProjectOverviewController(_dbContextFactory, _repository, _securityProvider);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private void AllowProjectAccess(string firebaseUid = "firebase-uid")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success(firebaseUid, "user-id-123")));
    }

    private void DenyProjectAccess(string errorMessage = "User does not have access to this project.")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure(errorMessage)));
    }

    // --- Identity / Access control ---

    [Test]
    public async Task GetProjectOverview_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.GetProjectOverview("proj-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetProjectOverview_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.GetProjectOverview("proj-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task GetProjectOverview_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.GetProjectOverview("proj-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Result shape ---

    [Test]
    public async Task GetProjectOverview_ReturnsOk_WhenUserHasAccess()
    {
        AllowProjectAccess();

        var result = await _controller.GetProjectOverview("proj-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetProjectOverview_ReturnsOpenTaskCount()
    {
        AllowProjectAccess();
        _repository.GetOpenTaskCountAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(7));

        var result   = (OkObjectResult)await _controller.GetProjectOverview("proj-1", CancellationToken.None);
        var response = result.Value as GetProjectOverviewResponse;

        Assert.That(response!.OpenTaskCount, Is.EqualTo(7));
    }

    [Test]
    public async Task GetProjectOverview_ReturnsNoteCount()
    {
        AllowProjectAccess();
        _repository.GetNoteCountAsync(_dbContext, Arg.Any<string>()).Returns(Task.FromResult(12));

        var result   = (OkObjectResult)await _controller.GetProjectOverview("proj-1", CancellationToken.None);
        var response = result.Value as GetProjectOverviewResponse;

        Assert.That(response!.NoteCount, Is.EqualTo(12));
    }

    [Test]
    public async Task GetProjectOverview_ReturnsDocumentCount()
    {
        AllowProjectAccess();
        _repository.GetDocumentCountAsync(_dbContext, Arg.Any<string>()).Returns(Task.FromResult(3));

        var result   = (OkObjectResult)await _controller.GetProjectOverview("proj-1", CancellationToken.None);
        var response = result.Value as GetProjectOverviewResponse;

        Assert.That(response!.DocumentCount, Is.EqualTo(3));
    }

    [Test]
    public async Task GetProjectOverview_QueriesOpenTasksWithCorrectFirebaseUidAndProjectId()
    {
        AllowProjectAccess("uid-abc");

        await _controller.GetProjectOverview("proj-xyz", CancellationToken.None);

        await _repository.Received(1).GetOpenTaskCountAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    [Test]
    public async Task GetProjectOverview_QueriesNotesWithCorrectProjectId()
    {
        AllowProjectAccess();

        await _controller.GetProjectOverview("proj-xyz", CancellationToken.None);

        await _repository.Received(1).GetNoteCountAsync(_dbContext, "proj-xyz");
    }

    [Test]
    public async Task GetProjectOverview_QueriesDocumentsWithCorrectProjectId()
    {
        AllowProjectAccess();

        await _controller.GetProjectOverview("proj-xyz", CancellationToken.None);

        await _repository.Received(1).GetDocumentCountAsync(_dbContext, "proj-xyz");
    }
}
