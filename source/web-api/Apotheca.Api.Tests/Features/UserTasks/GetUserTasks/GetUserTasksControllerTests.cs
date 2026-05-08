using Apotheca.Api.Features.UserTasks.GetUserTasks;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.UserTasks.GetUserTasks;

[TestFixture]
public class GetUserTasksControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private GetUserTasksRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private GetUserTasksController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<GetUserTasksRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new GetUserTasksController(_dbContextFactory, _repository, _securityProvider);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private static IEnumerable<GetUserTasksResponse> EmptyTasks() => Enumerable.Empty<GetUserTasksResponse>();

    private void AllowAccess(string firebaseUid = "firebase-uid")
    {
        _securityProvider
            .AuthorizeAccessAsync(_dbContext, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success(firebaseUid, "user-id-123")));
    }

    private void DenyAccess(string errorMessage = "User identity could not be determined.")
    {
        _securityProvider
            .AuthorizeAccessAsync(_dbContext, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure(errorMessage)));
    }

    // --- Identity / Access control ---

    [Test]
    public async Task GetUserTasks_Returns401_WhenIdentityFails()
    {
        DenyAccess();

        var result = await _controller.GetUserTasks(null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetUserTasks_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.GetUserTasks(null, CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task GetUserTasks_DoesNotQueryTasks_WhenAccessDenied()
    {
        DenyAccess();

        await _controller.GetUserTasks(null, CancellationToken.None);

        await _repository.DidNotReceive().GetAllOpenTasksAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
        await _repository.DidNotReceive().GetTasksDueTodayAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
        await _repository.DidNotReceive().GetTasksDueUpcomingAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    // --- Result shape ---

    [Test]
    public async Task GetUserTasks_ReturnsOk()
    {
        AllowAccess();
        _repository.GetAllOpenTasksAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        var result = await _controller.GetUserTasks(null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetUserTasks_ReturnsEmptyList_WhenNoTasksExist()
    {
        AllowAccess();
        _repository.GetAllOpenTasksAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        var result = (OkObjectResult)await _controller.GetUserTasks(null, CancellationToken.None);
        var tasks  = result.Value as IEnumerable<GetUserTasksResponse>;

        Assert.That(tasks, Is.Empty);
    }

    // --- Filter routing ---

    [Test]
    public async Task GetUserTasks_CallsGetAllOpenTasks_WhenFilterIsNull()
    {
        AllowAccess();
        _repository.GetAllOpenTasksAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetUserTasks(null, CancellationToken.None);

        await _repository.Received(1).GetAllOpenTasksAsync(_dbContext, "firebase-uid");
    }

    [Test]
    public async Task GetUserTasks_CallsGetAllOpenTasks_WhenFilterIsUnrecognised()
    {
        AllowAccess();
        _repository.GetAllOpenTasksAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetUserTasks("unknown", CancellationToken.None);

        await _repository.Received(1).GetAllOpenTasksAsync(_dbContext, "firebase-uid");
    }

    [Test]
    public async Task GetUserTasks_CallsGetTasksDueToday_WhenFilterIsToday()
    {
        AllowAccess();
        _repository.GetTasksDueTodayAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetUserTasks("today", CancellationToken.None);

        await _repository.Received(1).GetTasksDueTodayAsync(_dbContext, "firebase-uid");
    }

    [Test]
    public async Task GetUserTasks_CallsGetTasksDueToday_WhenFilterIsTodayUpperCase()
    {
        AllowAccess();
        _repository.GetTasksDueTodayAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetUserTasks("TODAY", CancellationToken.None);

        await _repository.Received(1).GetTasksDueTodayAsync(_dbContext, "firebase-uid");
    }

    [Test]
    public async Task GetUserTasks_CallsGetTasksDueUpcoming_WhenFilterIsUpcoming()
    {
        AllowAccess();
        _repository.GetTasksDueUpcomingAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetUserTasks("upcoming", CancellationToken.None);

        await _repository.Received(1).GetTasksDueUpcomingAsync(_dbContext, "firebase-uid");
    }

    [Test]
    public async Task GetUserTasks_CallsGetTasksDueUpcoming_WhenFilterIsUpcomingUpperCase()
    {
        AllowAccess();
        _repository.GetTasksDueUpcomingAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetUserTasks("UPCOMING", CancellationToken.None);

        await _repository.Received(1).GetTasksDueUpcomingAsync(_dbContext, "firebase-uid");
    }

    // --- Passthrough of identifiers ---

    [Test]
    public async Task GetUserTasks_PassesFirebaseUidToRepository()
    {
        AllowAccess("uid-abc");
        _repository.GetAllOpenTasksAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(EmptyTasks()));

        await _controller.GetUserTasks(null, CancellationToken.None);

        await _repository.Received(1).GetAllOpenTasksAsync(_dbContext, "uid-abc");
    }

    // --- Returned data ---

    [Test]
    public async Task GetUserTasks_ReturnsTasks()
    {
        AllowAccess();

        var repoResults = new[]
        {
            new GetUserTasksResponse { Id = "t1", Title = "First task",  ProjectId = "proj-1", ProjectName = "My Project", CreatedBy = "user-1" },
            new GetUserTasksResponse { Id = "t2", Title = "Second task", ProjectId = "proj-1", ProjectName = "My Project", CreatedBy = "user-1" },
        };
        _repository.GetAllOpenTasksAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<GetUserTasksResponse>>(repoResults));

        var result = (OkObjectResult)await _controller.GetUserTasks(null, CancellationToken.None);
        var tasks  = (result.Value as IEnumerable<GetUserTasksResponse>)!.ToList();

        Assert.That(tasks, Has.Count.EqualTo(2));
        Assert.That(tasks[0].Id, Is.EqualTo("t1"));
        Assert.That(tasks[1].Id, Is.EqualTo("t2"));
    }

    [Test]
    public async Task GetUserTasks_ReturnsProjectName_WithEachTask()
    {
        AllowAccess();

        var repoResults = new[]
        {
            new GetUserTasksResponse { Id = "t1", Title = "Task", ProjectId = "proj-1", ProjectName = "Apotheca", CreatedBy = "user-1" },
        };
        _repository.GetAllOpenTasksAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<GetUserTasksResponse>>(repoResults));

        var result = (OkObjectResult)await _controller.GetUserTasks(null, CancellationToken.None);
        var tasks  = (result.Value as IEnumerable<GetUserTasksResponse>)!.ToList();

        Assert.That(tasks[0].ProjectName, Is.EqualTo("Apotheca"));
    }
}
