using Apotheca.Api.Features.Documents.CreateDocument;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.CreateDocument;

[TestFixture]
public class CreateDocumentControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private CreateDocumentRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private CreateDocumentController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<CreateDocumentRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new CreateDocumentController(_dbContextFactory, _repository, _securityProvider, Substitute.For<ILogger<CreateDocumentController>>());
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

    // --- Identity / Access control ---

    [Test]
    public async Task CreateDocument_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.CreateDocument("proj-1", new CreateDocumentRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task CreateDocument_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.CreateDocument("proj-1", new CreateDocumentRequest(), CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task CreateDocument_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.CreateDocument("proj-1", new CreateDocumentRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Success ---

    [Test]
    public async Task CreateDocument_Returns201_WhenDocumentIsCreated()
    {
        AllowProjectAccess();
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-doc-id"));

        var result = await _controller.CreateDocument("proj-1", new CreateDocumentRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task CreateDocument_ReturnsNewId_WhenDocumentIsCreated()
    {
        AllowProjectAccess();
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-doc-id"));

        var result = (CreatedAtActionResult)await _controller.CreateDocument("proj-1", new CreateDocumentRequest(), CancellationToken.None);
        var id     = result.Value?.GetType().GetProperty("id")?.GetValue(result.Value)?.ToString();

        Assert.That(id, Is.EqualTo("new-doc-id"));
    }

    [Test]
    public async Task CreateDocument_CallsInsert_WithCorrectProjectIdAndUserId()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        await _controller.CreateDocument("proj-xyz", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.Received(1).InsertDocumentAsync(_dbContext, "proj-xyz", "user-id-xyz", Arg.Any<string?>());
    }

    [Test]
    public async Task CreateDocument_ForwardsParentDocumentId_ToRepository()
    {
        AllowProjectAccess();
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        var request = new CreateDocumentRequest { ParentDocumentId = "parent-folder-id" };
        await _controller.CreateDocument("proj-1", request, CancellationToken.None);

        await _repository.Received(1).InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), "parent-folder-id");
    }

    [Test]
    public async Task CreateDocument_PassesNullParentDocumentId_WhenNotProvided()
    {
        AllowProjectAccess();
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-id"));

        await _controller.CreateDocument("proj-1", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.Received(1).InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Is<string?>(x => x == null));
    }

    // --- Document log ---

    [Test]
    public async Task CreateDocument_WritesDocumentLog_WhenDocumentIsCreated()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-doc-id"));

        await _controller.CreateDocument("proj-xyz", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.Received(1).InsertDocumentLogAsync(_dbContext, "new-doc-id", "user-id-xyz", "proj-xyz");
    }

    [Test]
    public async Task CreateDocument_DoesNotWriteDocumentLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.CreateDocument("proj-xyz", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.DidNotReceive().InsertDocumentLogAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Activity log ---

    [Test]
    public async Task CreateDocument_WritesProjectActivityLog_WhenDocumentIsCreated()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-doc-id"));

        await _controller.CreateDocument("proj-xyz", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(_dbContext, "proj-xyz", "new-doc-id", "user-id-xyz", "Document added");
    }

    [Test]
    public async Task CreateDocument_DoesNotWriteActivityLog_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.CreateDocument("proj-xyz", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Search index ---

    [Test]
    public async Task CreateDocument_UpsertSearch_WhenDocumentIsCreated()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.InsertDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("new-doc-id"));

        await _controller.CreateDocument("proj-xyz", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.Received(1).UpsertSearchAsync(_dbContext, "proj-xyz", "new-doc-id", "New Document");
    }

    [Test]
    public async Task CreateDocument_DoesNotUpsertSearch_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.CreateDocument("proj-xyz", new CreateDocumentRequest(), CancellationToken.None);

        await _repository.DidNotReceive().UpsertSearchAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
