using Apotheca.Api.Features.Documents.RenameDocumentFolder;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.RenameDocumentFolder;

[TestFixture]
public class RenameDocumentFolderControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private RenameDocumentFolderRepository _repository = null!;
    private RenameDocumentFolderValidator _validator = null!;
    private ISecurityProvider _securityProvider = null!;
    private RenameDocumentFolderController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<RenameDocumentFolderRepository>();
        _validator        = Substitute.For<RenameDocumentFolderValidator>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _validator.Validate(Arg.Any<RenameDocumentFolderRequest>()).Returns([]);
        _repository.FolderExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(true));
        _repository.GetFolderTitleAsync(_dbContext, Arg.Any<string>()).Returns(Task.FromResult<string?>("Old Name"));

        _controller = new RenameDocumentFolderController(_dbContextFactory, _repository, _validator, _securityProvider, Substitute.For<ILogger<RenameDocumentFolderController>>());
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

    private static RenameDocumentFolderRequest ValidRequest() => new() { Title = "New Name" };

    // --- Validation ---

    [Test]
    public async Task RenameDocumentFolder_Returns400_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<RenameDocumentFolderRequest>()).Returns(["Folder name is required."]);

        var result = await _controller.RenameDocumentFolder("proj-1", "folder-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RenameDocumentFolder_ReturnsValidationErrors_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<RenameDocumentFolderRequest>()).Returns(["Folder name is required."]);

        var result = (BadRequestObjectResult)await _controller.RenameDocumentFolder("proj-1", "folder-1", ValidRequest(), CancellationToken.None);
        var errors = result.Value?.GetType().GetProperty("errors")?.GetValue(result.Value) as IReadOnlyList<string>;

        Assert.That(errors, Has.One.EqualTo("Folder name is required."));
    }

    [Test]
    public async Task RenameDocumentFolder_DoesNotOpenDatabase_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<RenameDocumentFolderRequest>()).Returns(["Folder name is required."]);

        await _controller.RenameDocumentFolder("proj-1", "folder-1", ValidRequest(), CancellationToken.None);

        await _dbContextFactory.DidNotReceive().CreateAsync(Arg.Any<CancellationToken>());
    }

    // --- Identity / Access control ---

    [Test]
    public async Task RenameDocumentFolder_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.RenameDocumentFolder("proj-1", "folder-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task RenameDocumentFolder_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.RenameDocumentFolder("proj-1", "folder-1", ValidRequest(), CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task RenameDocumentFolder_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.RenameDocumentFolder("proj-1", "folder-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Folder not found ---

    [Test]
    public async Task RenameDocumentFolder_Returns404_WhenFolderDoesNotExist()
    {
        AllowProjectAccess();
        _repository.FolderExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(false));

        var result = await _controller.RenameDocumentFolder("proj-1", "missing-folder", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task RenameDocumentFolder_ChecksFolderExistence_WithCorrectProjectAndFolderId()
    {
        AllowProjectAccess();

        await _controller.RenameDocumentFolder("proj-xyz", "folder-abc", ValidRequest(), CancellationToken.None);

        await _repository.Received(1).FolderExistsAsync(_dbContext, "proj-xyz", "folder-abc");
    }

    // --- Success ---

    [Test]
    public async Task RenameDocumentFolder_Returns200_WhenFolderIsRenamed()
    {
        AllowProjectAccess();

        var result = await _controller.RenameDocumentFolder("proj-1", "folder-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task RenameDocumentFolder_CallsRename_WithCorrectArguments()
    {
        AllowProjectAccess("user-id-xyz");

        await _controller.RenameDocumentFolder("proj-xyz", "folder-abc", new RenameDocumentFolderRequest { Title = "New Name" }, CancellationToken.None);

        await _repository.Received(1).RenameFolderAsync(_dbContext, "proj-xyz", "folder-abc", "New Name");
    }

    [Test]
    public async Task RenameDocumentFolder_TrimsTitle_BeforeCallingRepository()
    {
        AllowProjectAccess();

        await _controller.RenameDocumentFolder("proj-1", "folder-1", new RenameDocumentFolderRequest { Title = "  Trimmed Name  " }, CancellationToken.None);

        await _repository.Received(1).RenameFolderAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), "Trimmed Name");
    }

    [Test]
    public async Task RenameDocumentFolder_DoesNotRename_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.RenameDocumentFolder("proj-1", "folder-1", ValidRequest(), CancellationToken.None);

        await _repository.DidNotReceive().RenameFolderAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task RenameDocumentFolder_DoesNotRename_WhenFolderDoesNotExist()
    {
        AllowProjectAccess();
        _repository.FolderExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(false));

        await _controller.RenameDocumentFolder("proj-1", "folder-1", ValidRequest(), CancellationToken.None);

        await _repository.DidNotReceive().RenameFolderAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Activity log ---

    [Test]
    public async Task RenameDocumentFolder_WritesProjectActivityLog_WithFromAndToNames()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.GetFolderTitleAsync(_dbContext, "folder-abc").Returns(Task.FromResult<string?>("Old Name"));

        await _controller.RenameDocumentFolder("proj-xyz", "folder-abc", new RenameDocumentFolderRequest { Title = "New Name" }, CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(_dbContext, "proj-xyz", "folder-abc", "user-id-xyz", "Document folder 'Old Name' renamed to 'New Name'");
    }

    [Test]
    public async Task RenameDocumentFolder_ActivityLogMessage_UsesTrimmedNewTitle()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.GetFolderTitleAsync(_dbContext, Arg.Any<string>()).Returns(Task.FromResult<string?>("Old Name"));

        await _controller.RenameDocumentFolder("proj-xyz", "folder-abc", new RenameDocumentFolderRequest { Title = "  New Name  " }, CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), "Document folder 'Old Name' renamed to 'New Name'");
    }

    [Test]
    public async Task RenameDocumentFolder_DoesNotWriteProjectActivityLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.RenameDocumentFolder("proj-1", "folder-1", ValidRequest(), CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task RenameDocumentFolder_DoesNotWriteProjectActivityLog_WhenFolderDoesNotExist()
    {
        AllowProjectAccess();
        _repository.FolderExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(false));

        await _controller.RenameDocumentFolder("proj-1", "folder-1", ValidRequest(), CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
