using Apotheca.Api.Features.ProjectTasks.CompleteProjectTask;
using Apotheca.Api.Providers;
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
    private ISecurityProvider _securityProvider = null!;
    private CompleteProjectTaskController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<CompleteProjectTaskRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new CompleteProjectTaskController(_dbContextFactory, _repository, _securityProvider);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private void AllowProjectAccess()
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("firebase-uid", "user-id-123")));
    }

    private void DenyProjectAccess(string errorMessage = "User does not have access to this project.")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure(errorMessage)));
    }

    // --- Identity / Access control ---

    [Test]
    public async Task CompleteProjectTask_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.CompleteProjectTask("proj-1", "task-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task CompleteProjectTask_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.CompleteProjectTask("proj-1", "task-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task CompleteProjectTask_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.CompleteProjectTask("proj-1", "task-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task CompleteProjectTask_DoesNotCallComplete_WhenAccessDenied()
    {
        DenyProjectAccess();

        await _controller.CompleteProjectTask("proj-1", "task-1", CancellationToken.None);

        await _repository.DidNotReceive().CompleteTaskAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Not found ---

    [Test]
    public async Task CompleteProjectTask_Returns404_WhenTaskNotFound()
    {
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
        AllowProjectAccess();
        _repository.CompleteTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        var result = await _controller.CompleteProjectTask("proj-1", "task-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task CompleteProjectTask_CallsComplete_WithCorrectTaskIdAndProjectId()
    {
        AllowProjectAccess();
        _repository.CompleteTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        await _controller.CompleteProjectTask("proj-xyz", "task-abc", CancellationToken.None);

        await _repository.Received(1).CompleteTaskAsync(_dbContext, "task-abc", "proj-xyz");
    }
}
