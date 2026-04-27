using Apotheca.Api.Features.Documents.SaveDocument;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.SaveDocument;

[TestFixture]
public class SaveDocumentControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private SaveDocumentRepository _repository = null!;
    private SaveDocumentValidator _validator = null!;
    private ISecurityProvider _securityProvider = null!;
    private SaveDocumentController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<SaveDocumentRepository>();
        _validator        = Substitute.For<SaveDocumentValidator>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _validator.Validate(Arg.Any<SaveDocumentRequest>()).Returns([]);

        _controller = new SaveDocumentController(_dbContextFactory, _repository, _validator, _securityProvider);
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

    private void DocumentExists(bool exists = true)
    {
        _repository.DocumentExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(exists));
    }

    // --- Validation ---

    [Test]
    public async Task SaveDocument_Returns400_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveDocumentRequest>()).Returns(["At least one field must be provided."]);

        var result = await _controller.SaveDocument("proj-1", "doc-1", new SaveDocumentRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SaveDocument_DoesNotOpenDatabase_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveDocumentRequest>()).Returns(["At least one field must be provided."]);

        await _controller.SaveDocument("proj-1", "doc-1", new SaveDocumentRequest(), CancellationToken.None);

        await _dbContextFactory.DidNotReceive().CreateAsync(Arg.Any<CancellationToken>());
    }

    // --- Identity / Access control ---

    [Test]
    public async Task SaveDocument_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.SaveDocument("proj-1", "doc-1", new SaveDocumentRequest { Title = "Title" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SaveDocument_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.SaveDocument("proj-1", "doc-1", new SaveDocumentRequest { Title = "Title" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Document existence ---

    [Test]
    public async Task SaveDocument_Returns404_WhenDocumentDoesNotExist()
    {
        AllowProjectAccess();
        DocumentExists(false);

        var result = await _controller.SaveDocument("proj-1", "doc-1", new SaveDocumentRequest { Title = "Title" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // --- Success ---

    [Test]
    public async Task SaveDocument_Returns200_WhenDocumentIsUpdated()
    {
        AllowProjectAccess();
        DocumentExists();

        var result = await _controller.SaveDocument("proj-1", "doc-1", new SaveDocumentRequest { Title = "New Title" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    // --- Title update ---

    [Test]
    public async Task SaveDocument_CallsUpdateTitle_WhenTitleIsProvided()
    {
        AllowProjectAccess();
        DocumentExists();

        await _controller.SaveDocument("proj-1", "doc-1", new SaveDocumentRequest { Title = "New Title" }, CancellationToken.None);

        await _repository.Received(1).UpdateDocumentTitleAsync(_dbContext, "proj-1", "doc-1", "New Title");
    }

    [Test]
    public async Task SaveDocument_TrimsTitle_BeforeCallingRepository()
    {
        AllowProjectAccess();
        DocumentExists();

        await _controller.SaveDocument("proj-1", "doc-1", new SaveDocumentRequest { Title = "  My Doc  " }, CancellationToken.None);

        await _repository.Received(1).UpdateDocumentTitleAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), "My Doc");
    }

    [Test]
    public async Task SaveDocument_DoesNotCallUpdateTitle_WhenOnlyLabelsAreProvided()
    {
        AllowProjectAccess();
        DocumentExists();

        await _controller.SaveDocument("proj-1", "doc-1", new SaveDocumentRequest { Labels = ["tag"] }, CancellationToken.None);

        await _repository.DidNotReceive().UpdateDocumentTitleAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Label sync ---

    [Test]
    public async Task SaveDocument_DeletesAndResyncsLabels_WhenLabelsAreProvided()
    {
        AllowProjectAccess();
        DocumentExists();
        _repository.UpsertLabelAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult("label-id"));

        await _controller.SaveDocument("proj-1", "doc-1", new SaveDocumentRequest { Labels = ["tag1"] }, CancellationToken.None);

        await _repository.Received(1).DeleteDocumentLabelsAsync(_dbContext, "doc-1");
        await _repository.Received(1).UpsertLabelAsync(_dbContext, "proj-1", Arg.Any<string>(), "tag1");
        await _repository.Received(1).InsertDocumentLabelAsync(_dbContext, "doc-1", "label-id");
    }

    [Test]
    public async Task SaveDocument_DoesNotSyncLabels_WhenLabelsAreNull()
    {
        AllowProjectAccess();
        DocumentExists();

        await _controller.SaveDocument("proj-1", "doc-1", new SaveDocumentRequest { Title = "Title" }, CancellationToken.None);

        await _repository.DidNotReceive().DeleteDocumentLabelsAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    [Test]
    public async Task SaveDocument_ClearsAllLabels_WhenEmptyLabelsListIsProvided()
    {
        AllowProjectAccess();
        DocumentExists();

        await _controller.SaveDocument("proj-1", "doc-1", new SaveDocumentRequest { Labels = [] }, CancellationToken.None);

        await _repository.Received(1).DeleteDocumentLabelsAsync(_dbContext, "doc-1");
        await _repository.DidNotReceive().UpsertLabelAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
