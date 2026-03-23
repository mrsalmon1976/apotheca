using System.Security.Claims;
using Apotheca.Api.Features.ProjectTasks.CompleteProjectTask;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.ProjectTasks.CompleteProjectTask;

[TestFixture]
public class CompleteProjectTaskControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private CompleteProjectTaskRepository _repository = null!;
    private CompleteProjectTaskController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repository = Substitute.For<CompleteProjectTaskRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new CompleteProjectTaskController(_dbContextFactory, _repository);
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
    public async Task CompleteProjectTask_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.CompleteProjectTask("proj-1", "task-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task CompleteProjectTask_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.CompleteProjectTask("proj-1", "task-1", CancellationToken.None);
        var error = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task CompleteProjectTask_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.CompleteProjectTask("proj-1", "task-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task CompleteProjectTask_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.CompleteProjectTask("proj-xyz", "task-1", CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    [Test]
    public async Task CompleteProjectTask_DoesNotCallComplete_WhenAccessDenied()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.CompleteProjectTask("proj-1", "task-1", CancellationToken.None);

        await _repository.DidNotReceive().CompleteTaskAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Not found ---

    [Test]
    public async Task CompleteProjectTask_Returns404_WhenTaskNotFound()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.CompleteTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.CompleteProjectTask("proj-1", "task-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // --- Success ---

    [Test]
    public async Task CompleteProjectTask_Returns200_WhenTaskIsCompleted()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.CompleteTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        var result = await _controller.CompleteProjectTask("proj-1", "task-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task CompleteProjectTask_CallsComplete_WithCorrectTaskIdAndProjectId()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.CompleteTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        await _controller.CompleteProjectTask("proj-xyz", "task-abc", CancellationToken.None);

        await _repository.Received(1).CompleteTaskAsync(_dbContext, "task-abc", "proj-xyz");
    }
}
