using System.Security.Claims;
using Apotheca.Api.Features.ProjectTasks.GetProjectTasks;
using Apotheca.Data;
using Apotheca.Data.DbEntities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.ProjectTasks.GetProjectTasks;

[TestFixture]
public class GetProjectTasksControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private GetProjectTasksRepository _repository = null!;
    private GetProjectTasksController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repository = Substitute.For<GetProjectTasksRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new GetProjectTasksController(_dbContextFactory, _repository);
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

    private static IEnumerable<TaskDbEntity> EmptyTasks() => Enumerable.Empty<TaskDbEntity>();

    private void AllowProjectAccess()
    {
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
    }

    // --- Identity ---

    [Test]
    public async Task GetProjectTasks_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.GetProjectTasks("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetProjectTasks_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.GetProjectTasks("proj-1", null, CancellationToken.None);
        var error = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task GetProjectTasks_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.GetProjectTasks("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetProjectTasks_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.GetProjectTasks("proj-xyz", null, CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    [Test]
    public async Task GetProjectTasks_DoesNotQueryTasks_WhenAccessDenied()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.GetProjectTasks("proj-1", null, CancellationToken.None);

        await _repository.DidNotReceive().GetAllOpenTasksAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
        await _repository.DidNotReceive().GetTasksDueTodayAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
        await _repository.DidNotReceive().GetTasksDueUpcomingAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Result shape ---

    [Test]
    public async Task GetProjectTasks_ReturnsOk()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetAllOpenTasksAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        var result = await _controller.GetProjectTasks("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetProjectTasks_ReturnsEmptyList_WhenNoTasksExist()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetAllOpenTasksAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        var result = (OkObjectResult)await _controller.GetProjectTasks("proj-1", null, CancellationToken.None);
        var tasks = result.Value as IEnumerable<GetProjectTasksResponse>;

        Assert.That(tasks, Is.Empty);
    }

    // --- Filter routing ---

    [Test]
    public async Task GetProjectTasks_CallsGetAllOpenTasks_WhenFilterIsNull()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetAllOpenTasksAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetProjectTasks("proj-1", null, CancellationToken.None);

        await _repository.Received(1).GetAllOpenTasksAsync(_dbContext, "firebase-uid", "proj-1");
    }

    [Test]
    public async Task GetProjectTasks_CallsGetAllOpenTasks_WhenFilterIsUnrecognised()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetAllOpenTasksAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetProjectTasks("proj-1", "unknown", CancellationToken.None);

        await _repository.Received(1).GetAllOpenTasksAsync(_dbContext, "firebase-uid", "proj-1");
    }

    [Test]
    public async Task GetProjectTasks_CallsGetTasksDueToday_WhenFilterIsToday()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetTasksDueTodayAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetProjectTasks("proj-1", "today", CancellationToken.None);

        await _repository.Received(1).GetTasksDueTodayAsync(_dbContext, "firebase-uid", "proj-1");
    }

    [Test]
    public async Task GetProjectTasks_CallsGetTasksDueToday_WhenFilterIsTodayUpperCase()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetTasksDueTodayAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetProjectTasks("proj-1", "TODAY", CancellationToken.None);

        await _repository.Received(1).GetTasksDueTodayAsync(_dbContext, "firebase-uid", "proj-1");
    }

    [Test]
    public async Task GetProjectTasks_CallsGetTasksDueUpcoming_WhenFilterIsUpcoming()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetTasksDueUpcomingAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetProjectTasks("proj-1", "upcoming", CancellationToken.None);

        await _repository.Received(1).GetTasksDueUpcomingAsync(_dbContext, "firebase-uid", "proj-1");
    }

    [Test]
    public async Task GetProjectTasks_CallsGetTasksDueUpcoming_WhenFilterIsUpcomingUpperCase()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetTasksDueUpcomingAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetProjectTasks("proj-1", "UPCOMING", CancellationToken.None);

        await _repository.Received(1).GetTasksDueUpcomingAsync(_dbContext, "firebase-uid", "proj-1");
    }

    // --- Passthrough of identifiers ---

    [Test]
    public async Task GetProjectTasks_PassesFirebaseUidToRepository()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess();
        _repository.GetAllOpenTasksAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetProjectTasks("proj-1", null, CancellationToken.None);

        await _repository.Received(1).GetAllOpenTasksAsync(_dbContext, "uid-abc", Arg.Any<string>());
    }

    [Test]
    public async Task GetProjectTasks_PassesProjectIdToRepository()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetAllOpenTasksAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetProjectTasks("proj-xyz", null, CancellationToken.None);

        await _repository.Received(1).GetAllOpenTasksAsync(_dbContext, Arg.Any<string>(), "proj-xyz");
    }

    // --- Mapped response ---

    [Test]
    public async Task GetProjectTasks_ReturnsMappedTasks()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var dbResults = new[]
        {
            new TaskDbEntity { Id = "t1", Title = "First task",  ProjectId = "proj-1", CreatedBy = "user-1" },
            new TaskDbEntity { Id = "t2", Title = "Second task", ProjectId = "proj-1", CreatedBy = "user-1" },
        };
        _repository.GetAllOpenTasksAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<TaskDbEntity>>(dbResults));

        var result = (OkObjectResult)await _controller.GetProjectTasks("proj-1", null, CancellationToken.None);
        var tasks = (result.Value as IEnumerable<GetProjectTasksResponse>)!.ToList();

        Assert.That(tasks, Has.Count.EqualTo(2));
        Assert.That(tasks[0].Id, Is.EqualTo("t1"));
        Assert.That(tasks[1].Id, Is.EqualTo("t2"));
    }
}
