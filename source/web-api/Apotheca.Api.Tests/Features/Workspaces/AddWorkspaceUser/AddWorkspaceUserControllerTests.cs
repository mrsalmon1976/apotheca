using Apotheca.Api.Features.Workspaces.AddWorkspaceUser;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Workspaces.AddWorkspaceUser;

[TestFixture]
public class AddWorkspaceUserControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private AddWorkspaceUserRepository _repo = null!;
    private AddWorkspaceUserValidator _validator = null!;
    private ISecurityProvider _securityProvider = null!;
    private AddWorkspaceUserController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repo = Substitute.For<AddWorkspaceUserRepository>();
        _validator = Substitute.For<AddWorkspaceUserValidator>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "admin-user-id", DataConstants.WorkspaceRole.Admin)));
        _repo.GetUserIdByEmailAsync(_dbContext, Arg.Any<string>()).Returns(Task.FromResult<string?>("u2"));

        _controller = new AddWorkspaceUserController(_dbContextFactory, _repo, _validator, _securityProvider);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    [Test]
    public async Task AddWorkspaceUser_ReturnsBadRequest_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<AddWorkspaceUserRequest>()).Returns(new[] { "Email is required." });

        var result = await _controller.AddWorkspaceUser("ws-1",
            new AddWorkspaceUserRequest { Email = "", WorkspaceRole = DataConstants.WorkspaceRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task AddWorkspaceUser_ReturnsUnauthorized_WhenCallerIsNotWorkspaceAdmin()
    {
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure("Only workspace admins can perform this action.")));

        var result = await _controller.AddWorkspaceUser("ws-1",
            new AddWorkspaceUserRequest { Email = "a@b.com", WorkspaceRole = DataConstants.WorkspaceRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task AddWorkspaceUser_ReturnsBadRequest_WhenNoAccountExistsForEmail()
    {
        _repo.GetUserIdByEmailAsync(_dbContext, "a@b.com").Returns(Task.FromResult<string?>(null));

        var result = await _controller.AddWorkspaceUser("ws-1",
            new AddWorkspaceUserRequest { Email = "a@b.com", WorkspaceRole = DataConstants.WorkspaceRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task AddWorkspaceUser_ReturnsConflict_WhenUserIsAlreadyAMember()
    {
        _repo.IsMemberAsync(_dbContext, "ws-1", "u2").Returns(Task.FromResult(true));

        var result = await _controller.AddWorkspaceUser("ws-1",
            new AddWorkspaceUserRequest { Email = "a@b.com", WorkspaceRole = DataConstants.WorkspaceRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task AddWorkspaceUser_Succeeds_WhenUserExistsAndIsNotYetAMember()
    {
        _repo.IsMemberAsync(_dbContext, "ws-1", "u2").Returns(Task.FromResult(false));

        var result = await _controller.AddWorkspaceUser("ws-1",
            new AddWorkspaceUserRequest { Email = "a@b.com", WorkspaceRole = DataConstants.WorkspaceRole.Viewer },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
        await _repo.Received(1).AddMemberAsync(_dbContext, "ws-1", "u2", DataConstants.WorkspaceRole.Viewer);
    }

    [Test]
    public async Task AddWorkspaceUser_TrimsEmail_BeforeLookup()
    {
        await _controller.AddWorkspaceUser("ws-1",
            new AddWorkspaceUserRequest { Email = "  a@b.com  ", WorkspaceRole = DataConstants.WorkspaceRole.Viewer },
            CancellationToken.None);

        await _repo.Received(1).GetUserIdByEmailAsync(_dbContext, "a@b.com");
    }
}
