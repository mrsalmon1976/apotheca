using Apotheca.Api.Features.Projects.AddProjectUser;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Projects.AddProjectUser;

[TestFixture]
public class AddProjectUserControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private AddProjectUserRepository _repo = null!;
    private AddProjectUserValidator _validator = null!;
    private ISecurityProvider _securityProvider = null!;
    private AddProjectUserController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repo = Substitute.For<AddProjectUserRepository>();
        _validator = Substitute.For<AddProjectUserValidator>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _securityProvider.AuthorizeProjectAccessAsync(_dbContext, "proj-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "admin-user-id", DataConstants.ProjectRole.Admin)));
        _repo.GetWorkspaceIdForProjectAsync(_dbContext, "proj-1").Returns(Task.FromResult<string?>("ws-1"));

        _controller = new AddProjectUserController(_dbContextFactory, _repo, _validator, _securityProvider);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    [Test]
    public async Task AddProjectUser_ReturnsBadRequest_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<AddProjectUserRequest>()).Returns(new[] { "UserId is required." });

        var result = await _controller.AddProjectUser("proj-1",
            new AddProjectUserRequest { UserId = "", ProjectRole = DataConstants.ProjectRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task AddProjectUser_ReturnsUnauthorized_WhenCallerIsNotAuthorized()
    {
        _securityProvider.AuthorizeProjectAccessAsync(_dbContext, "proj-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure("User does not have access to this project.")));

        var result = await _controller.AddProjectUser("proj-1",
            new AddProjectUserRequest { UserId = "u2", ProjectRole = DataConstants.ProjectRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task AddProjectUser_ReturnsUnauthorized_WhenCallerIsNotProjectAdmin()
    {
        _securityProvider.AuthorizeProjectAccessAsync(_dbContext, "proj-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "user-id", DataConstants.ProjectRole.Contributor)));

        var result = await _controller.AddProjectUser("proj-1",
            new AddProjectUserRequest { UserId = "u2", ProjectRole = DataConstants.ProjectRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task AddProjectUser_ReturnsNotFound_WhenProjectDoesNotExist()
    {
        _repo.GetWorkspaceIdForProjectAsync(_dbContext, "proj-1").Returns(Task.FromResult<string?>(null));

        var result = await _controller.AddProjectUser("proj-1",
            new AddProjectUserRequest { UserId = "u2", ProjectRole = DataConstants.ProjectRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task AddProjectUser_ReturnsBadRequest_WhenUserIsNotAWorkspaceMember()
    {
        _repo.IsWorkspaceMemberAsync(_dbContext, "ws-1", "u2").Returns(Task.FromResult(false));

        var result = await _controller.AddProjectUser("proj-1",
            new AddProjectUserRequest { UserId = "u2", ProjectRole = DataConstants.ProjectRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task AddProjectUser_ReturnsConflict_WhenUserIsAlreadyAProjectMember()
    {
        _repo.IsWorkspaceMemberAsync(_dbContext, "ws-1", "u2").Returns(Task.FromResult(true));
        _repo.IsProjectMemberAsync(_dbContext, "proj-1", "u2").Returns(Task.FromResult(true));

        var result = await _controller.AddProjectUser("proj-1",
            new AddProjectUserRequest { UserId = "u2", ProjectRole = DataConstants.ProjectRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task AddProjectUser_Succeeds_WhenUserIsAWorkspaceMemberAndNotYetOnProject()
    {
        _repo.IsWorkspaceMemberAsync(_dbContext, "ws-1", "u2").Returns(Task.FromResult(true));
        _repo.IsProjectMemberAsync(_dbContext, "proj-1", "u2").Returns(Task.FromResult(false));

        var result = await _controller.AddProjectUser("proj-1",
            new AddProjectUserRequest { UserId = "u2", ProjectRole = DataConstants.ProjectRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
        await _repo.Received(1).AddMemberAsync(_dbContext, "proj-1", "u2", DataConstants.ProjectRole.Viewer);
    }
}
