using System.Security.Claims;
using Apotheca.Api.Features.ProjectTasks.SaveProjectTask;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.ProjectTasks.SaveProjectTask;

[TestFixture]
public class SaveProjectTaskControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private SaveProjectTaskRepository _repository = null!;
    private SaveProjectTaskValidator _validator = null!;
    private SaveProjectTaskController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repository = Substitute.For<SaveProjectTaskRepository>();
        _validator = Substitute.For<SaveProjectTaskValidator>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _validator.Validate(Arg.Any<SaveProjectTaskRequest>()).Returns([]);

        _controller = new SaveProjectTaskController(_dbContextFactory, _repository, _validator);
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

    private void AllowProjectAccess(string userId = "user-id-123")
    {
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        _repository.GetUserIdAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<string?>(userId));
    }

    private static SaveProjectTaskRequest NewTask(string? id = null) => new()
    {
        Id       = id,
        Title    = "Test task",
        Priority = "NONE",
    };

    // --- Validation ---

    [Test]
    public async Task SaveProjectTask_Returns400_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveProjectTaskRequest>()).Returns(["Title is required."]);

        var result = await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SaveProjectTask_ReturnsValidationErrors_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveProjectTaskRequest>()).Returns(["Title is required.", "Priority must be one of: NONE, LOW, MEDIUM, HIGH, URGENT."]);

        var result = (BadRequestObjectResult)await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);
        var errors = result.Value?.GetType().GetProperty("errors")?.GetValue(result.Value) as IReadOnlyList<string>;

        Assert.That(errors, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task SaveProjectTask_DoesNotCheckAccess_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveProjectTaskRequest>()).Returns(["Title is required."]);

        await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);

        await _repository.DidNotReceive().UserHasProjectAccessAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Identity ---

    [Test]
    public async Task SaveProjectTask_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SaveProjectTask_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);
        var error = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task SaveProjectTask_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task SaveProjectTask_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.SaveProjectTask("proj-xyz", NewTask(), CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    // --- Insert (new task) ---

    [Test]
    public async Task SaveProjectTask_Returns201_WhenTaskIsNew()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.InsertTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.FromResult("new-task-id"));

        var result = await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task SaveProjectTask_ReturnsNewId_WhenTaskIsNew()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.InsertTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.FromResult("new-task-id"));

        var result = (CreatedAtActionResult)await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);
        var id = result.Value?.GetType().GetProperty("id")?.GetValue(result.Value)?.ToString();

        Assert.That(id, Is.EqualTo("new-task-id"));
    }

    [Test]
    public async Task SaveProjectTask_Returns401_WhenUserIdCannotBeResolved()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        _repository.GetUserIdAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<string?>(null));

        var result = await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SaveProjectTask_CallsInsert_WithCorrectProjectIdAndUserId()
    {
        SetAuthenticatedUser("uid-abc");
        AllowProjectAccess("user-id-xyz");
        _repository.InsertTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.FromResult("new-id"));

        await _controller.SaveProjectTask("proj-xyz", NewTask(), CancellationToken.None);

        await _repository.Received(1).InsertTaskAsync(_dbContext, "proj-xyz", "user-id-xyz", Arg.Any<SaveProjectTaskRequest>());
    }

    [Test]
    public async Task SaveProjectTask_DoesNotCallUpdate_WhenTaskIsNew()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.InsertTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.FromResult("new-id"));

        await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);

        await _repository.DidNotReceive().UpdateTaskAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>());
    }

    // --- Update (existing task) ---

    [Test]
    public async Task SaveProjectTask_Returns200_WhenTaskIsExisting()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.UpdateTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.CompletedTask);

        var result = await _controller.SaveProjectTask("proj-1", NewTask("existing-id"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task SaveProjectTask_CallsUpdate_WithCorrectTaskIdAndProjectId()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.UpdateTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.CompletedTask);

        await _controller.SaveProjectTask("proj-xyz", NewTask("task-abc"), CancellationToken.None);

        await _repository.Received(1).UpdateTaskAsync(_dbContext, "task-abc", "proj-xyz", Arg.Any<SaveProjectTaskRequest>());
    }

    [Test]
    public async Task SaveProjectTask_DoesNotCallInsert_WhenTaskIsExisting()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.UpdateTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.CompletedTask);

        await _controller.SaveProjectTask("proj-1", NewTask("existing-id"), CancellationToken.None);

        await _repository.DidNotReceive().InsertTaskAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>());
    }

}
