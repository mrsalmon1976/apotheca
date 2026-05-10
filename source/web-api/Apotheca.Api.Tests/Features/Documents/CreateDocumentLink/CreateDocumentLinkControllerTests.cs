using Apotheca.Api.Features.Documents.CreateDocumentLink;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.CreateDocumentLink;

[TestFixture]
public class CreateDocumentLinkControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private CreateDocumentLinkRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private CreateDocumentLinkController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<CreateDocumentLinkRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _repository.DocumentExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(true));

        _controller = new CreateDocumentLinkController(_dbContextFactory, _repository, _securityProvider);
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

    private static CreateDocumentLinkResponse ALink(string id = "link-abc") => new()
    {
        Id        = id,
        CreatedAt = new DateTimeOffset(2025, 6, 1, 10, 0, 0, TimeSpan.Zero),
    };

    // --- Access control ---

    [Test]
    public async Task CreateDocumentLink_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.CreateDocumentLink("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task CreateDocumentLink_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.CreateDocumentLink("proj-1", "doc-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task CreateDocumentLink_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.CreateDocumentLink("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task CreateDocumentLink_DoesNotInsertLink_WhenAccessDenied()
    {
        DenyProjectAccess();

        await _controller.CreateDocumentLink("proj-1", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().InsertLinkAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Document existence ---

    [Test]
    public async Task CreateDocumentLink_Returns404_WhenDocumentDoesNotExist()
    {
        AllowProjectAccess();
        _repository.DocumentExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(false));

        var result = await _controller.CreateDocumentLink("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task CreateDocumentLink_ChecksDocumentWithCorrectProjectIdAndDocumentId()
    {
        AllowProjectAccess();
        _repository.DocumentExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(false));

        await _controller.CreateDocumentLink("proj-xyz", "doc-abc", CancellationToken.None);

        await _repository.Received(1).DocumentExistsAsync(_dbContext, "proj-xyz", "doc-abc");
    }

    [Test]
    public async Task CreateDocumentLink_DoesNotInsertLink_WhenDocumentDoesNotExist()
    {
        AllowProjectAccess();
        _repository.DocumentExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(false));

        await _controller.CreateDocumentLink("proj-1", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().InsertLinkAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Success ---

    [Test]
    public async Task CreateDocumentLink_Returns201_OnSuccess()
    {
        AllowProjectAccess();
        _repository.InsertLinkAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(ALink()));

        var result = await _controller.CreateDocumentLink("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedResult>());
    }

    [Test]
    public async Task CreateDocumentLink_ReturnsLinkData_OnSuccess()
    {
        AllowProjectAccess();
        var link = ALink("link-new");
        _repository.InsertLinkAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(link));

        var result   = (CreatedResult)await _controller.CreateDocumentLink("proj-1", "doc-1", CancellationToken.None);
        var response = result.Value as CreateDocumentLinkResponse;

        Assert.That(response,            Is.Not.Null);
        Assert.That(response!.Id,        Is.EqualTo("link-new"));
        Assert.That(response.CreatedAt,  Is.EqualTo(link.CreatedAt));
    }

    [Test]
    public async Task CreateDocumentLink_CallsInsert_WithCorrectDocumentIdAndUserId()
    {
        AllowProjectAccess("user-id-xyz");
        _repository.InsertLinkAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(ALink()));

        await _controller.CreateDocumentLink("proj-1", "doc-abc", CancellationToken.None);

        await _repository.Received(1).InsertLinkAsync(_dbContext, "doc-abc", "user-id-xyz");
    }
}
