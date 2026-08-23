using Apotheca.Api.Features.Projects.SaveProjectUserRole;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Projects.SaveProjectUserRole;

[TestFixture]
public class SaveProjectUserRoleControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private SaveProjectUserRoleRepository _repo = null!;
    private SaveProjectUserRoleValidator _validator = null!;
    private ISecurityProvider _securityProvider = null!;
    private SaveProjectUserRoleController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repo = Substitute.For<SaveProjectUserRoleRepository>();
        _validator = Substitute.For<SaveProjectUserRoleValidator>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _securityProvider.AuthorizeProjectAccessAsync(_dbContext, "proj-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "admin-user-id", DataConstants.ProjectRole.Admin)));

        _controller = new SaveProjectUserRoleController(_dbContextFactory, _repo, _validator, _securityProvider);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    [Test]
    public async Task SaveProjectUserRole_ReturnsBadRequest_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveProjectUserRoleRequest>()).Returns(new[] { "ProjectRole must be ADMIN, CONTRIBUTOR, or VIEWER." });

        var result = await _controller.SaveProjectUserRole("proj-1", "u2",
            new SaveProjectUserRoleRequest { ProjectRole = "SUPERUSER" },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SaveProjectUserRole_ReturnsUnauthorized_WhenCallerIsNotAuthorized()
    {
        _securityProvider.AuthorizeProjectAccessAsync(_dbContext, "proj-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure("User does not have access to this project.")));

        var result = await _controller.SaveProjectUserRole("proj-1", "u2",
            new SaveProjectUserRoleRequest { ProjectRole = DataConstants.ProjectRole.Contributor },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SaveProjectUserRole_ReturnsUnauthorized_WhenCallerIsNotProjectAdmin()
    {
        _securityProvider.AuthorizeProjectAccessAsync(_dbContext, "proj-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "user-id", DataConstants.ProjectRole.Contributor)));

        var result = await _controller.SaveProjectUserRole("proj-1", "u2",
            new SaveProjectUserRoleRequest { ProjectRole = DataConstants.ProjectRole.Contributor },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SaveProjectUserRole_Succeeds_WhenPromotingViewerToAdmin()
    {
        _repo.GetMemberRoleAsync(_dbContext, "proj-1", "u2").Returns(Task.FromResult<string?>(DataConstants.ProjectRole.Viewer));

        var result = await _controller.SaveProjectUserRole("proj-1", "u2",
            new SaveProjectUserRoleRequest { ProjectRole = DataConstants.ProjectRole.Admin },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
        await _repo.DidNotReceive().CountAdminsAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    [Test]
    public async Task SaveProjectUserRole_ReturnsBadRequest_WhenDemotingLastAdmin()
    {
        _repo.GetMemberRoleAsync(_dbContext, "proj-1", "u2").Returns(Task.FromResult<string?>(DataConstants.ProjectRole.Admin));
        _repo.CountAdminsAsync(_dbContext, "proj-1").Returns(Task.FromResult(1));

        var result = await _controller.SaveProjectUserRole("proj-1", "u2",
            new SaveProjectUserRoleRequest { ProjectRole = DataConstants.ProjectRole.Contributor },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SaveProjectUserRole_Succeeds_WhenDemotingAdmin_AndAnotherAdminExists()
    {
        _repo.GetMemberRoleAsync(_dbContext, "proj-1", "u2").Returns(Task.FromResult<string?>(DataConstants.ProjectRole.Admin));
        _repo.CountAdminsAsync(_dbContext, "proj-1").Returns(Task.FromResult(2));

        var result = await _controller.SaveProjectUserRole("proj-1", "u2",
            new SaveProjectUserRoleRequest { ProjectRole = DataConstants.ProjectRole.Contributor },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task SaveProjectUserRole_ReturnsNotFound_WhenUserIsNotAMember()
    {
        _repo.GetMemberRoleAsync(_dbContext, "proj-1", "u2").Returns(Task.FromResult<string?>(null));

        var result = await _controller.SaveProjectUserRole("proj-1", "u2",
            new SaveProjectUserRoleRequest { ProjectRole = DataConstants.ProjectRole.Contributor },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }
}
