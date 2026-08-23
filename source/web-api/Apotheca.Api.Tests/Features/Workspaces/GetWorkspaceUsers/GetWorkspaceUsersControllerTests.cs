using Apotheca.Api.Features.Workspaces.GetWorkspaceUsers;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Apotheca.Data.DbEntities;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Workspaces.GetWorkspaceUsers;

[TestFixture]
public class GetWorkspaceUsersControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private GetWorkspaceUsersRepository _repo = null!;
    private ISecurityProvider _securityProvider = null!;
    private GetWorkspaceUsersController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repo = Substitute.For<GetWorkspaceUsersRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "user-id", DataConstants.WorkspaceRole.Viewer)));
        _repo.GetMembersAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(Enumerable.Empty<WorkspaceUserDbEntity>()));

        _controller = new GetWorkspaceUsersController(_dbContextFactory, _repo, _securityProvider);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    [Test]
    public async Task GetWorkspaceUsers_ReturnsUnauthorized_WhenCallerIsNotAuthorized()
    {
        _securityProvider.AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure("User does not have access to this workspace.")));

        var result = await _controller.GetWorkspaceUsers("ws-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetWorkspaceUsers_DoesNotRequireAdmin()
    {
        await _controller.GetWorkspaceUsers("ws-1", CancellationToken.None);

        await _securityProvider.Received(1).AuthorizeWorkspaceAccessAsync(_dbContext, "ws-1", false, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetWorkspaceUsers_ReturnsOk()
    {
        var result = await _controller.GetWorkspaceUsers("ws-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetWorkspaceUsers_ReturnsEmptyList_WhenWorkspaceHasNoMembers()
    {
        var result = (OkObjectResult)await _controller.GetWorkspaceUsers("ws-1", CancellationToken.None);
        var users  = result.Value as IEnumerable<GetWorkspaceUsersResponse>;

        Assert.That(users, Is.Empty);
    }

    [Test]
    public async Task GetWorkspaceUsers_ReturnsMappedUsers()
    {
        _repo.GetMembersAsync(_dbContext, "ws-1").Returns(Task.FromResult<IEnumerable<WorkspaceUserDbEntity>>(new[]
        {
            new WorkspaceUserDbEntity { UserId = "u1", Email = "a@b.com", DisplayName = "Alice", WorkspaceRole = DataConstants.WorkspaceRole.Admin },
            new WorkspaceUserDbEntity { UserId = "u2", Email = "c@d.com", DisplayName = "Carol", WorkspaceRole = DataConstants.WorkspaceRole.Viewer },
        }));

        var result = (OkObjectResult)await _controller.GetWorkspaceUsers("ws-1", CancellationToken.None);
        var users  = (result.Value as IEnumerable<GetWorkspaceUsersResponse>)!.ToList();

        Assert.That(users, Has.Count.EqualTo(2));
        Assert.That(users[0].UserId, Is.EqualTo("u1"));
        Assert.That(users[1].UserId, Is.EqualTo("u2"));
    }

    [Test]
    public async Task GetWorkspaceUsers_QueriesMembersWithWorkspaceId()
    {
        await _controller.GetWorkspaceUsers("ws-1", CancellationToken.None);

        await _repo.Received(1).GetMembersAsync(_dbContext, "ws-1");
    }
}
