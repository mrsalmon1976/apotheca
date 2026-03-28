using System.Security.Claims;
using Apotheca.Api.Features.Labels.SearchLabels;
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
    private SearchLabelsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repository = Substitute.For<SearchLabelsRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new SearchLabelsController(_dbContextFactory, _repository);
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

    private void AllowProjectAccess()
    {
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        _repository.SearchAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(Enumerable.Empty<SearchLabelsResponse>()));
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
        var items = result.Value as IEnumerable<SearchLabelsResponse>;

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

    // --- Identity ---

    [Test]
    public async Task SearchLabels_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.SearchLabels("proj-1", "plan", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SearchLabels_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.SearchLabels("proj-1", "plan", CancellationToken.None);
        var error = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task SearchLabels_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.SearchLabels("proj-1", "plan", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task SearchLabels_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.SearchLabels("proj-xyz", "plan", CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    [Test]
    public async Task SearchLabels_DoesNotSearch_WhenAccessDenied()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.SearchLabels("proj-1", "plan", CancellationToken.None);

        await _repository.DidNotReceive().SearchAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Query forwarding ---

    [Test]
    public async Task SearchLabels_PassesProjectIdAndTrimmedQuery_ToRepository()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        await _controller.SearchLabels("proj-xyz", "  plan  ", CancellationToken.None);

        await _repository.Received(1).SearchAsync(_dbContext, "proj-xyz", "plan");
    }

    [Test]
    public async Task SearchLabels_TrimsQuery_BeforePassingToRepository()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        await _controller.SearchLabels("proj-1", "  frontend  ", CancellationToken.None);

        await _repository.Received(1).SearchAsync(_dbContext, Arg.Any<string>(), "frontend");
    }

    // --- Result shape ---

    [Test]
    public async Task SearchLabels_ReturnsOk_WhenQueryIsProvided()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var result = await _controller.SearchLabels("proj-1", "plan", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task SearchLabels_ReturnsEmptyList_WhenNoLabelsMatch()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var result = (OkObjectResult)await _controller.SearchLabels("proj-1", "plan", CancellationToken.None);
        var items = result.Value as IEnumerable<SearchLabelsResponse>;

        Assert.That(items, Is.Empty);
    }

    [Test]
    public async Task SearchLabels_ReturnsMappedLabels()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        var repoResults = new[]
        {
            new SearchLabelsResponse { Id = "lbl-1", LabelText = "planning" },
            new SearchLabelsResponse { Id = "lbl-2", LabelText = "platform" },
        };
        _repository.SearchAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<SearchLabelsResponse>>(repoResults));

        var result = (OkObjectResult)await _controller.SearchLabels("proj-1", "pla", CancellationToken.None);
        var items = (result.Value as IEnumerable<SearchLabelsResponse>)!.ToList();

        Assert.That(items, Has.Count.EqualTo(2));
        Assert.That(items[0].Id, Is.EqualTo("lbl-1"));
        Assert.That(items[0].LabelText, Is.EqualTo("planning"));
        Assert.That(items[1].Id, Is.EqualTo("lbl-2"));
        Assert.That(items[1].LabelText, Is.EqualTo("platform"));
    }
}
