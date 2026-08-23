using Apotheca.Api.Features.Workspaces.SaveWorkspace;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Workspaces.SaveWorkspace;

[TestFixture]
public class SaveWorkspaceControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private SaveWorkspaceRepository _repo = null!;
    private SaveWorkspaceValidator _validator = null!;
    private ISecurityProvider _securityProvider = null!;
    private SaveWorkspaceController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repo = Substitute.For<SaveWorkspaceRepository>();
        _validator = Substitute.For<SaveWorkspaceValidator>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "admin-user-id", DataConstants.WorkspaceRole.Admin)));
        _repo.SaveWorkspaceAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(true));

        _controller = new SaveWorkspaceController(_dbContextFactory, _repo, _validator, _securityProvider);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    [Test]
    public async Task SaveWorkspace_ReturnsBadRequest_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveWorkspaceRequest>()).Returns(new[] { "Name is required." });

        var result = await _controller.SaveWorkspace("ws-1", new SaveWorkspaceRequest { Name = "" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SaveWorkspace_ReturnsUnauthorized_WhenCallerIsNotWorkspaceAdmin()
    {
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure("Only workspace admins can perform this action.")));

        var result = await _controller.SaveWorkspace("ws-1", new SaveWorkspaceRequest { Name = "Acme Corp" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SaveWorkspace_ReturnsNotFound_WhenWorkspaceDoesNotExist()
    {
        _repo.SaveWorkspaceAsync(_dbContext, "ws-1", Arg.Any<string>()).Returns(Task.FromResult(false));

        var result = await _controller.SaveWorkspace("ws-1", new SaveWorkspaceRequest { Name = "Acme Corp" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task SaveWorkspace_ReturnsOk_WhenSaved()
    {
        var result = await _controller.SaveWorkspace("ws-1", new SaveWorkspaceRequest { Name = "Acme Corp" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task SaveWorkspace_TrimsName_BeforeSaving()
    {
        await _controller.SaveWorkspace("ws-1", new SaveWorkspaceRequest { Name = "  Acme Corp  " }, CancellationToken.None);

        await _repo.Received(1).SaveWorkspaceAsync(_dbContext, "ws-1", "Acme Corp");
    }
}
