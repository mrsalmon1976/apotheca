using Apotheca.Api.Features.Documents.GetDocumentLinks;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.GetDocumentLinks;

[TestFixture]
public class GetDocumentLinksControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private GetDocumentLinksRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private GetDocumentLinksController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<GetDocumentLinksRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new GetDocumentLinksController(_dbContextFactory, _repository, _securityProvider);
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
    public async Task GetDocumentLinks_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.GetDocumentLinks("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetDocumentLinks_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.GetDocumentLinks("proj-1", "doc-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task GetDocumentLinks_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.GetDocumentLinks("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetDocumentLinks_DoesNotQueryLinks_WhenAccessDenied()
    {
        DenyProjectAccess();

        await _controller.GetDocumentLinks("proj-1", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().GetLinksAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Success ---

    [Test]
    public async Task GetDocumentLinks_ReturnsOk_WhenAccessGranted()
    {
        AllowProjectAccess();
        _repository.GetLinksAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<GetDocumentLinksResponse>>([]));

        var result = await _controller.GetDocumentLinks("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetDocumentLinks_ReturnsEmptyList_WhenNoLinksExist()
    {
        AllowProjectAccess();
        _repository.GetLinksAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<GetDocumentLinksResponse>>([]));

        var result   = (OkObjectResult)await _controller.GetDocumentLinks("proj-1", "doc-1", CancellationToken.None);
        var links    = result.Value as IEnumerable<GetDocumentLinksResponse>;

        Assert.That(links, Is.Empty);
    }

    [Test]
    public async Task GetDocumentLinks_ReturnsLinks_WhenLinksExist()
    {
        AllowProjectAccess();

        var createdAt = new DateTimeOffset(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var linkList  = new List<GetDocumentLinksResponse>
        {
            new() { Id = "link-abc", CreatedAt = createdAt },
            new() { Id = "link-xyz", CreatedAt = createdAt.AddHours(-1) },
        };
        _repository.GetLinksAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<GetDocumentLinksResponse>>(linkList));

        var result = (OkObjectResult)await _controller.GetDocumentLinks("proj-1", "doc-1", CancellationToken.None);
        var links  = (result.Value as IEnumerable<GetDocumentLinksResponse>)?.ToList();

        Assert.That(links,            Is.Not.Null);
        Assert.That(links!.Count,     Is.EqualTo(2));
        Assert.That(links[0].Id,      Is.EqualTo("link-abc"));
        Assert.That(links[0].CreatedAt, Is.EqualTo(createdAt));
        Assert.That(links[1].Id,      Is.EqualTo("link-xyz"));
    }

    [Test]
    public async Task GetDocumentLinks_QueriesWithCorrectProjectIdAndDocumentId()
    {
        AllowProjectAccess();
        _repository.GetLinksAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<GetDocumentLinksResponse>>([]));

        await _controller.GetDocumentLinks("proj-xyz", "doc-abc", CancellationToken.None);

        await _repository.Received(1).GetLinksAsync(_dbContext, "proj-xyz", "doc-abc");
    }
}
