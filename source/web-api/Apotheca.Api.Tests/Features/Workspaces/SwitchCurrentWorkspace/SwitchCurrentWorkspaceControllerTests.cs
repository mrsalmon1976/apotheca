using Apotheca.Api.Features.Workspaces.SwitchCurrentWorkspace;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Workspaces.SwitchCurrentWorkspace;

[TestFixture]
public class SwitchCurrentWorkspaceControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private SwitchCurrentWorkspaceRepository _repo = null!;
    private ISecurityProvider _securityProvider = null!;
    private SwitchCurrentWorkspaceController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repo = Substitute.For<SwitchCurrentWorkspaceRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "user-id-123", DataConstants.WorkspaceRole.Viewer)));

        _controller = new SwitchCurrentWorkspaceController(_dbContextFactory, _repo, _securityProvider);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    [Test]
    public async Task SwitchCurrentWorkspace_ReturnsUnauthorized_WhenCallerIsNotAuthorized()
    {
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure("User does not have access to this workspace.")));

        var result = await _controller.SwitchCurrentWorkspace("ws-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SwitchCurrentWorkspace_DoesNotRequireAdmin()
    {
        await _controller.SwitchCurrentWorkspace("ws-1", CancellationToken.None);

        await _securityProvider.Received(1).AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", false, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SwitchCurrentWorkspace_ReturnsOk()
    {
        var result = await _controller.SwitchCurrentWorkspace("ws-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task SwitchCurrentWorkspace_SetsCurrentWorkspace_ForCallingUser()
    {
        await _controller.SwitchCurrentWorkspace("ws-1", CancellationToken.None);

        await _repo.Received(1).SetCurrentWorkspaceAsync(_dbContext, "user-id-123", "ws-1");
    }

    [Test]
    public async Task SwitchCurrentWorkspace_DoesNotSetCurrentWorkspace_WhenNotAuthorized()
    {
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure("User does not have access to this workspace.")));

        await _controller.SwitchCurrentWorkspace("ws-1", CancellationToken.None);

        await _repo.DidNotReceive().SetCurrentWorkspaceAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
