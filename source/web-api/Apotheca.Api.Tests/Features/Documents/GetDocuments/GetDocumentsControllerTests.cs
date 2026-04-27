using Apotheca.Api.Features.Documents.GetDocuments;
using Apotheca.Api.Providers;
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
    private ISecurityProvider _securityProvider = null!;
    private GetDocumentsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<GetDocumentsRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _repository.GetDocumentsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult<IEnumerable<GetDocumentsResponse>>([]));

        _controller = new GetDocumentsController(_dbContextFactory, _repository, _securityProvider);
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

    // --- Identity / Access control ---

    [Test]
    public async Task GetDocuments_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.GetDocuments("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetDocuments_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.GetDocuments("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Success ---

    [Test]
    public async Task GetDocuments_ReturnsOk_WhenAccessIsAllowed()
    {
        AllowProjectAccess();

        var result = await _controller.GetDocuments("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetDocuments_QueriesWithCorrectProjectIdAndNullParent()
    {
        AllowProjectAccess();

        await _controller.GetDocuments("proj-xyz", null, CancellationToken.None);

        await _repository.Received(1).GetDocumentsAsync(_dbContext, "proj-xyz", null);
    }

    [Test]
    public async Task GetDocuments_QueriesWithCorrectProjectIdAndParentId()
    {
        AllowProjectAccess();

        await _controller.GetDocuments("proj-xyz", "folder-abc", CancellationToken.None);

        await _repository.Received(1).GetDocumentsAsync(_dbContext, "proj-xyz", "folder-abc");
    }

    [Test]
    public async Task GetDocuments_ReturnsDocumentList()
    {
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

    [Test]
    public async Task GetDocuments_ReturnsMimetypeOnDocuments()
    {
        AllowProjectAccess();

        var documents = new List<GetDocumentsResponse>
        {
            new() { Id = "doc-1", Title = "Spec", IsFolder = false, Mimetype = "application/pdf" },
        };
        _repository.GetDocumentsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult<IEnumerable<GetDocumentsResponse>>(documents));

        var result   = (OkObjectResult)await _controller.GetDocuments("proj-1", null, CancellationToken.None);
        var response = (result.Value as IEnumerable<GetDocumentsResponse>)!.ToList();

        Assert.That(response[0].Mimetype, Is.EqualTo("application/pdf"));
    }
}
