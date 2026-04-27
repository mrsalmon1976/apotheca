using Apotheca.Api.Features.Projects.SaveProject;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Projects.SaveProject;

[TestFixture]
public class SaveProjectControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private SaveProjectRepository _repository = null!;
    private SaveProjectValidator _validator = null!;
    private ISecurityProvider _securityProvider = null!;
    private SaveProjectController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<SaveProjectRepository>();
        _validator        = Substitute.For<SaveProjectValidator>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _validator.Validate(Arg.Any<SaveProjectRequest>()).Returns([]);

        _controller = new SaveProjectController(_dbContextFactory, _repository, _validator, _securityProvider);
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

    private static SaveProjectRequest ValidRequest() => new()
    {
        Name    = "My Project",
        Summary = "A brief description.",
    };

    // --- Validation ---

    [Test]
    public async Task SaveProject_Returns400_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveProjectRequest>()).Returns(["Name is required."]);

        var result = await _controller.SaveProject("proj-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SaveProject_ReturnsValidationErrors_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveProjectRequest>()).Returns(["Name is required."]);

        var result = (BadRequestObjectResult)await _controller.SaveProject("proj-1", ValidRequest(), CancellationToken.None);
        var errors = result.Value?.GetType().GetProperty("errors")?.GetValue(result.Value) as IReadOnlyList<string>;

        Assert.That(errors, Has.One.EqualTo("Name is required."));
    }

    [Test]
    public async Task SaveProject_DoesNotOpenDatabase_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveProjectRequest>()).Returns(["Name is required."]);

        await _controller.SaveProject("proj-1", ValidRequest(), CancellationToken.None);

        await _dbContextFactory.DidNotReceive().CreateAsync(Arg.Any<CancellationToken>());
    }

    // --- Identity / Access control ---

    [Test]
    public async Task SaveProject_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.SaveProject("proj-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SaveProject_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.SaveProject("proj-1", ValidRequest(), CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task SaveProject_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.SaveProject("proj-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Success ---

    [Test]
    public async Task SaveProject_Returns200_WhenProjectIsSaved()
    {
        AllowProjectAccess();
        _repository.SaveProjectAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult(true));

        var result = await _controller.SaveProject("proj-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task SaveProject_TrimsNameAndSummary_BeforeCallingRepository()
    {
        AllowProjectAccess();
        _repository.SaveProjectAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult(true));

        var request = new SaveProjectRequest { Name = "  Trimmed Name  ", Summary = "  Trimmed Summary  " };
        await _controller.SaveProject("proj-xyz", request, CancellationToken.None);

        await _repository.Received(1).SaveProjectAsync(_dbContext, "proj-xyz", "Trimmed Name", "Trimmed Summary");
    }

    [Test]
    public async Task SaveProject_PassesNullSummary_WhenSummaryIsNull()
    {
        AllowProjectAccess();
        _repository.SaveProjectAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult(true));

        var request = new SaveProjectRequest { Name = "My Project", Summary = null };
        await _controller.SaveProject("proj-xyz", request, CancellationToken.None);

        await _repository.Received(1).SaveProjectAsync(_dbContext, "proj-xyz", "My Project", null);
    }

    // --- Not found ---

    [Test]
    public async Task SaveProject_Returns404_WhenProjectIsNotFound()
    {
        AllowProjectAccess();
        _repository.SaveProjectAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult(false));

        var result = await _controller.SaveProject("proj-1", ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task SaveProject_Returns404_WithErrorMessage_ContainingProjectId()
    {
        AllowProjectAccess();
        _repository.SaveProjectAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult(false));

        var result = (NotFoundObjectResult)await _controller.SaveProject("not-found-id", ValidRequest(), CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Does.Contain("not-found-id"));
    }
}
