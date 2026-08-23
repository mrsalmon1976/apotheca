using Apotheca.Api.Features.Projects.CreateProject;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Projects.CreateProject;

[TestFixture]
public class CreateProjectControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private CreateProjectRepository _repo = null!;
    private CreateProjectValidator _validator = null!;
    private ISecurityProvider _securityProvider = null!;
    private CreateProjectController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repo = Substitute.For<CreateProjectRepository>();
        _validator = Substitute.For<CreateProjectValidator>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "admin-user-id", DataConstants.WorkspaceRole.Admin)));
        _repo.CreateProjectAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult("proj-1"));

        _controller = new CreateProjectController(_dbContextFactory, _repo, _validator, _securityProvider);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private static CreateProjectRequest ValidRequest(IReadOnlyList<CreateProjectMemberRequest>? members = null) => new()
    {
        WorkspaceId = "ws-1",
        Name = "Project",
        Members = members ?? [],
    };

    [Test]
    public async Task CreateProject_ReturnsBadRequest_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<CreateProjectRequest>()).Returns(new[] { "Name is required." });

        var result = await _controller.CreateProject(ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateProject_ReturnsUnauthorized_WhenCallerIsNotWorkspaceAdmin()
    {
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure("Only workspace admins can perform this action.")));

        var result = await _controller.CreateProject(ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task CreateProject_ReturnsBadRequest_WhenMemberIsNotAWorkspaceMember()
    {
        _repo.IsWorkspaceMemberAsync(_dbContext, "ws-1", "u2").Returns(Task.FromResult(false));

        var request = ValidRequest([new CreateProjectMemberRequest { UserId = "u2", ProjectRole = DataConstants.ProjectRole.Viewer }]);
        var result = await _controller.CreateProject(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        await _repo.DidNotReceive().CreateProjectAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>());
    }

    [Test]
    public async Task CreateProject_ReturnsOk_WithProjectIdAndName()
    {
        var result = (OkObjectResult)await _controller.CreateProject(ValidRequest(), CancellationToken.None);
        var id = result.Value?.GetType().GetProperty("id")?.GetValue(result.Value)?.ToString();
        var name = result.Value?.GetType().GetProperty("name")?.GetValue(result.Value)?.ToString();

        Assert.That(id, Is.EqualTo("proj-1"));
        Assert.That(name, Is.EqualTo("Project"));
    }

    [Test]
    public async Task CreateProject_TrimsNameAndSummary_BeforeCreating()
    {
        var request = new CreateProjectRequest { WorkspaceId = "ws-1", Name = "  Project  ", Summary = "  A summary  " };

        await _controller.CreateProject(request, CancellationToken.None);

        await _repo.Received(1).CreateProjectAsync(_dbContext, "ws-1", "Project", "A summary");
    }

    [Test]
    public async Task CreateProject_AddsCreatorAsAdmin()
    {
        await _controller.CreateProject(ValidRequest(), CancellationToken.None);

        await _repo.Received(1).AddProjectMemberAsync(_dbContext, "proj-1", "admin-user-id", DataConstants.ProjectRole.Admin);
    }

    [Test]
    public async Task CreateProject_AddsEachRequestedMember_WithTheirRole()
    {
        _repo.IsWorkspaceMemberAsync(_dbContext, "ws-1", "u2").Returns(Task.FromResult(true));
        _repo.IsWorkspaceMemberAsync(_dbContext, "ws-1", "u3").Returns(Task.FromResult(true));

        var request = ValidRequest([
            new CreateProjectMemberRequest { UserId = "u2", ProjectRole = DataConstants.ProjectRole.Contributor },
            new CreateProjectMemberRequest { UserId = "u3", ProjectRole = DataConstants.ProjectRole.Viewer },
        ]);

        var result = await _controller.CreateProject(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        await _repo.Received(1).AddProjectMemberAsync(_dbContext, "proj-1", "admin-user-id", DataConstants.ProjectRole.Admin);
        await _repo.Received(1).AddProjectMemberAsync(_dbContext, "proj-1", "u2", DataConstants.ProjectRole.Contributor);
        await _repo.Received(1).AddProjectMemberAsync(_dbContext, "proj-1", "u3", DataConstants.ProjectRole.Viewer);
    }
}
