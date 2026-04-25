using System.Security.Claims;
using Apotheca.Api.Features.Documents.GetDocument;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Documents.GetDocument;

[TestFixture]
public class GetDocumentControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private GetDocumentRepository _repository = null!;
    private GetDocumentController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<GetDocumentRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new GetDocumentController(_dbContextFactory, _repository);
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

    private static GetDocumentResponse ADocument(string id = "doc-1") => new()
    {
        Id       = id,
        Title    = "My Document",
        IsFolder = false,
    };

    // --- Identity ---

    [Test]
    public async Task GetDocument_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.GetDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetDocument_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.GetDocument("proj-1", "doc-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task GetDocument_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.GetDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetDocument_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.GetDocument("proj-xyz", "doc-1", CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    [Test]
    public async Task GetDocument_DoesNotQueryDocument_WhenAccessDenied()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.GetDocument("proj-1", "doc-1", CancellationToken.None);

        await _repository.DidNotReceive().GetDocumentAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Not found ---

    [Test]
    public async Task GetDocument_Returns404_WhenDocumentDoesNotExist()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetDocumentResponse?>(null));

        var result = await _controller.GetDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetDocument_QueriesWithCorrectProjectIdAndDocumentId()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetDocumentResponse?>(null));

        await _controller.GetDocument("proj-xyz", "doc-abc", CancellationToken.None);

        await _repository.Received(1).GetDocumentAsync(_dbContext, "proj-xyz", "doc-abc");
    }

    // --- Success ---

    [Test]
    public async Task GetDocument_ReturnsOk_WhenDocumentExists()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetDocumentResponse?>(ADocument()));

        var result = await _controller.GetDocument("proj-1", "doc-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetDocument_ReturnsMappedDocument()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var document = new GetDocumentResponse
        {
            Id               = "doc-abc",
            ParentDocumentId = "folder-id",
            IsFolder         = false,
            Title            = "Project Spec",
            FileName         = "spec.pdf",
            FileExtension    = ".pdf",
            Mimetype         = "application/pdf",
            FileLength       = 204800,
            Labels           = ["alpha", "beta"],
            CreatedAt        = new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero),
            UpdatedAt        = new DateTimeOffset(2025, 1, 16, 12, 0, 0, TimeSpan.Zero),
        };
        _repository.GetDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetDocumentResponse?>(document));

        var result   = (OkObjectResult)await _controller.GetDocument("proj-1", "doc-abc", CancellationToken.None);
        var response = result.Value as GetDocumentResponse;

        Assert.That(response,                 Is.Not.Null);
        Assert.That(response!.Id,             Is.EqualTo("doc-abc"));
        Assert.That(response.ParentDocumentId, Is.EqualTo("folder-id"));
        Assert.That(response.IsFolder,        Is.False);
        Assert.That(response.Title,           Is.EqualTo("Project Spec"));
        Assert.That(response.FileName,        Is.EqualTo("spec.pdf"));
        Assert.That(response.FileExtension,   Is.EqualTo(".pdf"));
        Assert.That(response.Mimetype,        Is.EqualTo("application/pdf"));
        Assert.That(response.FileLength,      Is.EqualTo(204800));
        Assert.That(response.Labels,          Is.EqualTo(new[] { "alpha", "beta" }));
    }

    [Test]
    public async Task GetDocument_ReturnsMappedFolder()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var folder = new GetDocumentResponse { Id = "folder-abc", IsFolder = true, Title = "Specs" };
        _repository.GetDocumentAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetDocumentResponse?>(folder));

        var result   = (OkObjectResult)await _controller.GetDocument("proj-1", "folder-abc", CancellationToken.None);
        var response = result.Value as GetDocumentResponse;

        Assert.That(response!.IsFolder,        Is.True);
        Assert.That(response.FileName,         Is.Null);
        Assert.That(response.ParentDocumentId, Is.Null);
        Assert.That(response.Labels,           Is.Empty);
    }
}
