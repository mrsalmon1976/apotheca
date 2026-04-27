using Apotheca.Api.Features.Notes.SaveNoteFolder;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Notes.SaveNoteFolder;

[TestFixture]
public class SaveNoteFolderControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private SaveNoteFolderRepository _repository = null!;
    private SaveNoteFolderValidator _validator = null!;
    private ISecurityProvider _securityProvider = null!;
    private SaveNoteFolderController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<SaveNoteFolderRepository>();
        _validator        = Substitute.For<SaveNoteFolderValidator>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _validator.Validate(Arg.Any<SaveNoteFolderRequest>()).Returns([]);

        _controller = new SaveNoteFolderController(_dbContextFactory, _repository, _validator, _securityProvider, Substitute.For<ILogger<SaveNoteFolderController>>());
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

    private static SaveNoteFolderRequest ValidRequest() => new() { Title = "Meeting Notes" };

    // --- Validation ---

    [Test]
    public async Task SaveNoteFolder_Returns400_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveNoteFolderRequest>()).Returns(["Folder name is required."]);

        var result = await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SaveNoteFolder_ReturnsValidationErrors_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveNoteFolderRequest>()).Returns(["Folder name is required."]);

        var result = (BadRequestObjectResult)await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);
        var errors = result.Value?.GetType().GetProperty("errors")?.GetValue(result.Value) as IReadOnlyList<string>;

        Assert.That(errors, Has.One.EqualTo("Folder name is required."));
    }

    [Test]
    public async Task SaveNoteFolder_DoesNotOpenDatabase_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveNoteFolderRequest>()).Returns(["Folder name is required."]);

        await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);

        await _dbContextFactory.DidNotReceive().CreateAsync(Arg.Any<CancellationToken>());
    }

    // --- Identity / Access control ---

    [Test]
    public async Task SaveNoteFolder_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SaveNoteFolder_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task SaveNoteFolder_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Success ---

    [Test]
    public async Task SaveNoteFolder_Returns201_WhenFolderIsCreated()
    {
        AllowProjectAccess();
        _repository.InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-folder-id"));

        var result = await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task SaveNoteFolder_ReturnsNewId_WhenFolderIsCreated()
    {
        AllowProjectAccess();
        _repository.InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-folder-id"));

        var result = (CreatedAtActionResult)await _controller.SaveNoteFolder("proj-1", ValidRequest(), CancellationToken.None);
        var id     = result.Value?.GetType().GetProperty("id")?.GetValue(result.Value)?.ToString();

        Assert.That(id, Is.EqualTo("new-folder-id"));
    }

    [Test]
    public async Task SaveNoteFolder_CallsInsert_WithCorrectProjectIdAndUserId()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        await _controller.SaveNoteFolder("proj-xyz", ValidRequest(), CancellationToken.None);

        await _repository.Received(1).InsertNoteFolderAsync(_dbContext, "proj-xyz", "user-id-xyz", Arg.Any<string>(), Arg.Any<string?>());
    }

    [Test]
    public async Task SaveNoteFolder_TrimsTitle_BeforeCallingRepository()
    {
        AllowProjectAccess();
        _repository.InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        var request = new SaveNoteFolderRequest { Title = "  My Folder  " };
        await _controller.SaveNoteFolder("proj-1", request, CancellationToken.None);

        await _repository.Received(1).InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), "My Folder", Arg.Any<string?>());
    }

    [Test]
    public async Task SaveNoteFolder_ForwardsParentNoteId_ToRepository()
    {
        AllowProjectAccess();
        _repository.InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        var request = new SaveNoteFolderRequest { Title = "Sub Folder", ParentNoteId = "parent-folder-id" };
        await _controller.SaveNoteFolder("proj-1", request, CancellationToken.None);

        await _repository.Received(1).InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), "parent-folder-id");
    }

    // --- Note log ---

    [Test]
    public async Task SaveNoteFolder_WritesNoteLog_WhenFolderIsCreated()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-folder-id"));

        await _controller.SaveNoteFolder("proj-xyz", ValidRequest(), CancellationToken.None);

        await _repository.Received(1).InsertNoteLogAsync(_dbContext, "new-folder-id", "user-id-xyz", "proj-xyz");
    }

    [Test]
    public async Task SaveNoteFolder_DoesNotWriteNoteLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.SaveNoteFolder("proj-xyz", ValidRequest(), CancellationToken.None);

        await _repository.DidNotReceive().InsertNoteLogAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Activity log ---

    [Test]
    public async Task SaveNoteFolder_WritesProjectActivityLog_WhenFolderIsCreated()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-folder-id"));

        await _controller.SaveNoteFolder("proj-xyz", new SaveNoteFolderRequest { Title = "Meeting Notes" }, CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(_dbContext, "proj-xyz", "new-folder-id", "user-id-xyz", "Note folder 'Meeting Notes' added");
    }

    [Test]
    public async Task SaveNoteFolder_ActivityLogMessage_UsesTrimmedTitle()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.InsertNoteFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-folder-id"));

        await _controller.SaveNoteFolder("proj-xyz", new SaveNoteFolderRequest { Title = "  Meeting Notes  " }, CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), "Note folder 'Meeting Notes' added");
    }

    [Test]
    public async Task SaveNoteFolder_DoesNotWriteProjectActivityLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.SaveNoteFolder("proj-xyz", ValidRequest(), CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
