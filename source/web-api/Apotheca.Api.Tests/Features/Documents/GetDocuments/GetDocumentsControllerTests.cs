using System.Security.Claims;
using Apotheca.Api.Features.Documents.GetDocuments;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.GetDocuments;

[TestFixture]
public class GetDocumentsControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private GetDocumentsRepository _repository = null!;
    private GetDocumentsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<GetDocumentsRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _repository.GetDocumentsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult<IEnumerable<GetDocumentsResponse>>([]));

        _controller = new GetDocumentsController(_dbContextFactory, _repository);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private void SetAuthenticatedUser(string firebaseUid)
    {
        var claims   = new[] { new Claim("sub", firebaseUid) };
        var identity = new ClaimsIdentity(claims, "test");
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
    }

    private void AllowProjectAccess()
    {
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
    }

    // --- Identity ---

    [Test]
    public async Task GetDocuments_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.GetDocuments("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Access control ---

    [Test]
    public async Task GetDocuments_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.GetDocuments("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetDocuments_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.GetDocuments("proj-xyz", null, CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    // --- Success ---

    [Test]
    public async Task GetDocuments_ReturnsOk_WhenAccessIsAllowed()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var result = await _controller.GetDocuments("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetDocuments_QueriesWithCorrectProjectIdAndNullParent()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        await _controller.GetDocuments("proj-xyz", null, CancellationToken.None);

        await _repository.Received(1).GetDocumentsAsync(_dbContext, "proj-xyz", null);
    }

    [Test]
    public async Task GetDocuments_QueriesWithCorrectProjectIdAndParentId()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        await _controller.GetDocuments("proj-xyz", "folder-abc", CancellationToken.None);

        await _repository.Received(1).GetDocumentsAsync(_dbContext, "proj-xyz", "folder-abc");
    }

    [Test]
    public async Task GetDocuments_ReturnsDocumentList()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var documents = new List<GetDocumentsResponse>
        {
            new() { Id = "doc-1", Title = "Spec", IsFolder = false },
            new() { Id = "folder-1", Title = "Archive", IsFolder = true },
        };
        _repository.GetDocumentsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult<IEnumerable<GetDocumentsResponse>>(documents));

        var result   = (OkObjectResult)await _controller.GetDocuments("proj-1", null, CancellationToken.None);
        var response = result.Value as IEnumerable<GetDocumentsResponse>;

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Count(), Is.EqualTo(2));
    }
}
