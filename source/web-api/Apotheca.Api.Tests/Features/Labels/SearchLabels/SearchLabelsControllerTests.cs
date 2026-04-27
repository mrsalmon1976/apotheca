using Apotheca.Api.Features.Labels.SearchLabels;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Labels.SearchLabels;

[TestFixture]
public class SearchLabelsControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private SearchLabelsRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private SearchLabelsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<SearchLabelsRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new SearchLabelsController(_dbContextFactory, _repository, _securityProvider);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private void AllowProjectAccess()
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("firebase-uid", "user-id-123")));
        _repository.SearchAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(Enumerable.Empty<SearchLabelsResponse>()));
    }

    private void DenyProjectAccess(string errorMessage = "User does not have access to this project.")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure(errorMessage)));
    }

    // --- Empty query short-circuit ---

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task SearchLabels_ReturnsOkWithEmptyList_WhenQueryIsNullOrWhitespace(string? q)
    {
        var result = await _controller.SearchLabels("proj-1", q, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task SearchLabels_ReturnsEmptyList_WhenQueryIsNullOrWhitespace(string? q)
    {
        var result = (OkObjectResult)await _controller.SearchLabels("proj-1", q, CancellationToken.None);
        var items  = result.Value as IEnumerable<SearchLabelsResponse>;

        Assert.That(items, Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task SearchLabels_DoesNotOpenDatabase_WhenQueryIsNullOrWhitespace(string? q)
    {
        await _controller.SearchLabels("proj-1", q, CancellationToken.None);

        await _dbContextFactory.DidNotReceive().CreateAsync(Arg.Any<CancellationToken>());
    }

    // --- Identity / Access control ---

    [Test]
    public async Task SearchLabels_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.SearchLabels("proj-1", "plan", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SearchLabels_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.SearchLabels("proj-1", "plan", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task SearchLabels_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.SearchLabels("proj-1", "plan", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SearchLabels_DoesNotSearch_WhenAccessDenied()
    {
        DenyProjectAccess();

        await _controller.SearchLabels("proj-1", "plan", CancellationToken.None);

        await _repository.DidNotReceive().SearchAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Query forwarding ---

    [Test]
    public async Task SearchLabels_PassesProjectIdAndTrimmedQuery_ToRepository()
    {
        AllowProjectAccess();

        await _controller.SearchLabels("proj-xyz", "  plan  ", CancellationToken.None);

        await _repository.Received(1).SearchAsync(_dbContext, "proj-xyz", "plan");
    }

    [Test]
    public async Task SearchLabels_TrimsQuery_BeforePassingToRepository()
    {
        AllowProjectAccess();

        await _controller.SearchLabels("proj-1", "  frontend  ", CancellationToken.None);

        await _repository.Received(1).SearchAsync(_dbContext, Arg.Any<string>(), "frontend");
    }

    // --- Result shape ---

    [Test]
    public async Task SearchLabels_ReturnsOk_WhenQueryIsProvided()
    {
        AllowProjectAccess();

        var result = await _controller.SearchLabels("proj-1", "plan", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task SearchLabels_ReturnsEmptyList_WhenNoLabelsMatch()
    {
        AllowProjectAccess();

        var result = (OkObjectResult)await _controller.SearchLabels("proj-1", "plan", CancellationToken.None);
        var items  = result.Value as IEnumerable<SearchLabelsResponse>;

        Assert.That(items, Is.Empty);
    }

    [Test]
    public async Task SearchLabels_ReturnsMappedLabels()
    {
        AllowProjectAccess();

        var repoResults = new[]
        {
            new SearchLabelsResponse { Id = "lbl-1", LabelText = "planning" },
            new SearchLabelsResponse { Id = "lbl-2", LabelText = "platform" },
        };
        _repository.SearchAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<SearchLabelsResponse>>(repoResults));

        var result = (OkObjectResult)await _controller.SearchLabels("proj-1", "pla", CancellationToken.None);
        var items  = (result.Value as IEnumerable<SearchLabelsResponse>)!.ToList();

        Assert.That(items, Has.Count.EqualTo(2));
        Assert.That(items[0].Id,        Is.EqualTo("lbl-1"));
        Assert.That(items[0].LabelText, Is.EqualTo("planning"));
        Assert.That(items[1].Id,        Is.EqualTo("lbl-2"));
        Assert.That(items[1].LabelText, Is.EqualTo("platform"));
    }
}
