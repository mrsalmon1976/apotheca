using Apotheca.Api.Features.Workspaces.RemoveWorkspaceUser;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Workspaces.RemoveWorkspaceUser;

[TestFixture]
public class RemoveWorkspaceUserControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private RemoveWorkspaceUserRepository _repo = null!;
    private ISecurityProvider _securityProvider = null!;
    private RemoveWorkspaceUserController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repo = Substitute.For<RemoveWorkspaceUserRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "admin-user-id", DataConstants.WorkspaceRole.Admin)));

        _controller = new RemoveWorkspaceUserController(_dbContextFactory, _repo, _securityProvider);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    [Test]
    public async Task RemoveWorkspaceUser_ReturnsUnauthorized_WhenCallerIsNotWorkspaceAdmin()
    {
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure("Only workspace admins can perform this action.")));

        var result = await _controller.RemoveWorkspaceUser("ws-1", "user-2", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task RemoveWorkspaceUser_ReturnsNotFound_WhenUserIsNotAMember()
    {
        _repo.GetMemberRoleAsync(_dbContext, "ws-1", "user-2").Returns(Task.FromResult<string?>(null));

        var result = await _controller.RemoveWorkspaceUser("ws-1", "user-2", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task RemoveWorkspaceUser_ReturnsBadRequest_WhenRemovingLastAdmin()
    {
        _repo.GetMemberRoleAsync(_dbContext, "ws-1", "user-2").Returns(Task.FromResult<string?>(DataConstants.WorkspaceRole.Admin));
        _repo.CountAdminsAsync(_dbContext, "ws-1").Returns(Task.FromResult(1));

        var result = await _controller.RemoveWorkspaceUser("ws-1", "user-2", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        await _dbContext.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RemoveWorkspaceUser_Succeeds_WhenRemovingViewer()
    {
        _repo.GetMemberRoleAsync(_dbContext, "ws-1", "user-2").Returns(Task.FromResult<string?>(DataConstants.WorkspaceRole.Viewer));

        var result = await _controller.RemoveWorkspaceUser("ws-1", "user-2", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
        await _repo.Received(1).RemoveMemberAsync(_dbContext, "ws-1", "user-2");
    }

    [Test]
    public async Task RemoveWorkspaceUser_RemovesProjectAccess_WhenRemovingMember()
    {
        _repo.GetMemberRoleAsync(_dbContext, "ws-1", "user-2").Returns(Task.FromResult<string?>(DataConstants.WorkspaceRole.Viewer));

        await _controller.RemoveWorkspaceUser("ws-1", "user-2", CancellationToken.None);

        await _repo.Received(1).RemoveProjectAccessForWorkspaceAsync(_dbContext, "ws-1", "user-2");
    }

    [Test]
    public async Task RemoveWorkspaceUser_ReassignsCurrentWorkspace_WhenRemovingMember()
    {
        _repo.GetMemberRoleAsync(_dbContext, "ws-1", "user-2").Returns(Task.FromResult<string?>(DataConstants.WorkspaceRole.Viewer));

        await _controller.RemoveWorkspaceUser("ws-1", "user-2", CancellationToken.None);

        await _repo.Received(1).ReassignCurrentWorkspaceAsync(_dbContext, "user-2", "ws-1");
    }

    [Test]
    public async Task RemoveWorkspaceUser_Succeeds_WhenRemovingAdmin_AndAnotherAdminExists()
    {
        _repo.GetMemberRoleAsync(_dbContext, "ws-1", "user-2").Returns(Task.FromResult<string?>(DataConstants.WorkspaceRole.Admin));
        _repo.CountAdminsAsync(_dbContext, "ws-1").Returns(Task.FromResult(2));

        var result = await _controller.RemoveWorkspaceUser("ws-1", "user-2", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }
}
