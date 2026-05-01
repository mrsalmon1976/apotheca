using Apotheca.Api.Features.ProjectTasks.SaveProjectTask;
using Apotheca.Api.Providers;
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
    private ISecurityProvider _securityProvider = null!;
    private SaveProjectTaskController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<SaveProjectTaskRepository>();
        _validator        = Substitute.For<SaveProjectTaskValidator>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _validator.Validate(Arg.Any<SaveProjectTaskRequest>()).Returns([]);

        _controller = new SaveProjectTaskController(_dbContextFactory, _repository, _validator, _securityProvider);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private void AllowProjectAccess(string userId = "user-id-123")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("firebase-uid", userId)));
    }

    private void DenyProjectAccess(string errorMessage = "User does not have access to this project.")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure(errorMessage)));
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

        await _securityProvider.DidNotReceive().AuthorizeProjectAccessAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // --- Identity / Access control ---

    [Test]
    public async Task SaveProjectTask_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SaveProjectTask_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task SaveProjectTask_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Insert (new task) ---

    [Test]
    public async Task SaveProjectTask_Returns201_WhenTaskIsNew()
    {
        AllowProjectAccess();
        _repository.InsertTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.FromResult("new-task-id"));

        var result = await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task SaveProjectTask_ReturnsNewId_WhenTaskIsNew()
    {
        AllowProjectAccess();
        _repository.InsertTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.FromResult("new-task-id"));

        var result = (CreatedAtActionResult)await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);
        var id     = result.Value?.GetType().GetProperty("id")?.GetValue(result.Value)?.ToString();

        Assert.That(id, Is.EqualTo("new-task-id"));
    }

    [Test]
    public async Task SaveProjectTask_CallsInsert_WithCorrectProjectIdAndUserId()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.InsertTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.FromResult("new-id"));

        await _controller.SaveProjectTask("proj-xyz", NewTask(), CancellationToken.None);

        await _repository.Received(1).InsertTaskAsync(_dbContext, "proj-xyz", "user-id-xyz", Arg.Any<SaveProjectTaskRequest>());
    }

    [Test]
    public async Task SaveProjectTask_DoesNotCallUpdate_WhenTaskIsNew()
    {
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
        AllowProjectAccess();
        _repository.UpdateTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.CompletedTask);

        var result = await _controller.SaveProjectTask("proj-1", NewTask("existing-id"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task SaveProjectTask_CallsUpdate_WithCorrectTaskIdAndProjectId()
    {
        AllowProjectAccess();
        _repository.UpdateTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.CompletedTask);

        await _controller.SaveProjectTask("proj-xyz", NewTask("task-abc"), CancellationToken.None);

        await _repository.Received(1).UpdateTaskAsync(_dbContext, "task-abc", "proj-xyz", Arg.Any<SaveProjectTaskRequest>());
    }

    [Test]
    public async Task SaveProjectTask_DoesNotCallInsert_WhenTaskIsExisting()
    {
        AllowProjectAccess();
        _repository.UpdateTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.CompletedTask);

        await _controller.SaveProjectTask("proj-1", NewTask("existing-id"), CancellationToken.None);

        await _repository.DidNotReceive().InsertTaskAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>());
    }

    // --- Search ---

    [Test]
    public async Task SaveProjectTask_UpsertsSearchRecord_WhenTaskIsNew()
    {
        AllowProjectAccess();
        _repository.InsertTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.FromResult("new-task-id"));

        await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);

        await _repository.Received(1).UpsertSearchAsync(_dbContext, "proj-1", "new-task-id", "Test task", "");
    }

    [Test]
    public async Task SaveProjectTask_UpsertsSearchRecord_WhenTaskIsExisting()
    {
        AllowProjectAccess();
        _repository.UpdateTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.CompletedTask);

        await _controller.SaveProjectTask("proj-1", NewTask("existing-id"), CancellationToken.None);

        await _repository.Received(1).UpsertSearchAsync(_dbContext, "proj-1", "existing-id", "Test task", "");
    }

    [Test]
    public async Task SaveProjectTask_UpsertsSearchRecord_WithNotes_WhenNotesAreProvided()
    {
        AllowProjectAccess();
        _repository.InsertTaskAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SaveProjectTaskRequest>())
            .Returns(Task.FromResult("new-task-id"));

        var request = new SaveProjectTaskRequest { Title = "Task with notes", Notes = "Some notes", Priority = "NONE" };
        await _controller.SaveProjectTask("proj-1", request, CancellationToken.None);

        await _repository.Received(1).UpsertSearchAsync(_dbContext, "proj-1", "new-task-id", "Task with notes", "Some notes");
    }

    [Test]
    public async Task SaveProjectTask_DoesNotUpsertSearchRecord_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.SaveProjectTask("proj-1", NewTask(), CancellationToken.None);

        await _repository.DidNotReceive().UpsertSearchAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
