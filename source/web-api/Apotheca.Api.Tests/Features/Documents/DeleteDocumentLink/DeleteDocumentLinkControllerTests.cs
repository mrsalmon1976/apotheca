using Apotheca.Api.Features.Documents.DeleteDocumentLink;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.DeleteDocumentLink;

[TestFixture]
public class DeleteDocumentLinkControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private DeleteDocumentLinkRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private DeleteDocumentLinkController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<DeleteDocumentLinkRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new DeleteDocumentLinkController(_dbContextFactory, _repository, _securityProvider);
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

    // --- Access control ---

    [Test]
    public async Task DeleteDocumentLink_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.DeleteDocumentLink("proj-1", "doc-1", "link-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task DeleteDocumentLink_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.DeleteDocumentLink("proj-1", "doc-1", "link-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task DeleteDocumentLink_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.DeleteDocumentLink("proj-1", "doc-1", "link-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task DeleteDocumentLink_DoesNotDelete_WhenAccessDenied()
    {
        DenyProjectAccess();

        await _controller.DeleteDocumentLink("proj-1", "doc-1", "link-1", CancellationToken.None);

        await _repository.DidNotReceive().DeleteLinkAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Not found ---

    [Test]
    public async Task DeleteDocumentLink_Returns404_WhenLinkDoesNotExist()
    {
        AllowProjectAccess();
        _repository.DeleteLinkAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.DeleteDocumentLink("proj-1", "doc-1", "link-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // --- Success ---

    [Test]
    public async Task DeleteDocumentLink_Returns204_WhenLinkIsDeleted()
    {
        AllowProjectAccess();
        _repository.DeleteLinkAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        var result = await _controller.DeleteDocumentLink("proj-1", "doc-1", "link-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteDocumentLink_CallsDeleteWithCorrectParameters()
    {
        AllowProjectAccess();
        _repository.DeleteLinkAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        await _controller.DeleteDocumentLink("proj-xyz", "doc-abc", "link-def", CancellationToken.None);

        await _repository.Received(1).DeleteLinkAsync(_dbContext, "proj-xyz", "doc-abc", "link-def");
    }
}
