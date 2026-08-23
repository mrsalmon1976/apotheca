using Apotheca.Api.Features.Workspaces.SaveWorkspaceUserRole;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Workspaces.SaveWorkspaceUserRole;

[TestFixture]
public class SaveWorkspaceUserRoleControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private SaveWorkspaceUserRoleRepository _repo = null!;
    private SaveWorkspaceUserRoleValidator _validator = null!;
    private ISecurityProvider _securityProvider = null!;
    private SaveWorkspaceUserRoleController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repo = Substitute.For<SaveWorkspaceUserRoleRepository>();
        _validator = Substitute.For<SaveWorkspaceUserRoleValidator>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "admin-user-id", DataConstants.WorkspaceRole.Admin)));

        _controller = new SaveWorkspaceUserRoleController(_dbContextFactory, _repo, _validator, _securityProvider);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    [Test]
    public async Task SaveWorkspaceUserRole_ReturnsBadRequest_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveWorkspaceUserRoleRequest>()).Returns(new[] { "WorkspaceRole must be ADMIN or VIEWER." });

        var result = await _controller.SaveWorkspaceUserRole("ws-1", "user-2",
            new SaveWorkspaceUserRoleRequest { WorkspaceRole = "SUPERUSER" },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SaveWorkspaceUserRole_ReturnsNotFound_WhenUserIsNotAMember()
    {
        _repo.GetMemberRoleAsync(_dbContext, "ws-1", "user-2").Returns(Task.FromResult<string?>(null));

        var result = await _controller.SaveWorkspaceUserRole("ws-1", "user-2",
            new SaveWorkspaceUserRoleRequest { WorkspaceRole = DataConstants.WorkspaceRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task SaveWorkspaceUserRole_ReturnsBadRequest_WhenDemotingLastAdmin()
    {
        _repo.GetMemberRoleAsync(_dbContext, "ws-1", "user-2").Returns(Task.FromResult<string?>(DataConstants.WorkspaceRole.Admin));
        _repo.CountAdminsAsync(_dbContext, "ws-1").Returns(Task.FromResult(1));

        var result = await _controller.SaveWorkspaceUserRole("ws-1", "user-2",
            new SaveWorkspaceUserRoleRequest { WorkspaceRole = DataConstants.WorkspaceRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SaveWorkspaceUserRole_Succeeds_WhenDemotingAdmin_AndAnotherAdminExists()
    {
        _repo.GetMemberRoleAsync(_dbContext, "ws-1", "user-2").Returns(Task.FromResult<string?>(DataConstants.WorkspaceRole.Admin));
        _repo.CountAdminsAsync(_dbContext, "ws-1").Returns(Task.FromResult(2));

        var result = await _controller.SaveWorkspaceUserRole("ws-1", "user-2",
            new SaveWorkspaceUserRoleRequest { WorkspaceRole = DataConstants.WorkspaceRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task SaveWorkspaceUserRole_Succeeds_WhenPromotingViewerToAdmin()
    {
        _repo.GetMemberRoleAsync(_dbContext, "ws-1", "user-2").Returns(Task.FromResult<string?>(DataConstants.WorkspaceRole.Viewer));

        var result = await _controller.SaveWorkspaceUserRole("ws-1", "user-2",
            new SaveWorkspaceUserRoleRequest { WorkspaceRole = DataConstants.WorkspaceRole.Admin },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
        await _repo.DidNotReceive().CountAdminsAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    [Test]
    public async Task SaveWorkspaceUserRole_ReturnsUnauthorized_WhenCallerIsNotWorkspaceAdmin()
    {
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure("Only workspace admins can perform this action.")));

        var result = await _controller.SaveWorkspaceUserRole("ws-1", "user-2",
            new SaveWorkspaceUserRoleRequest { WorkspaceRole = DataConstants.WorkspaceRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }
}
