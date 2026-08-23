using Apotheca.Api.Features.Projects.GetProjectUsers;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Apotheca.Data.DbEntities;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Projects.GetProjectUsers;

[TestFixture]
public class GetProjectUsersControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private GetProjectUsersRepository _repo = null!;
    private ISecurityProvider _securityProvider = null!;
    private GetProjectUsersController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repo = Substitute.For<GetProjectUsersRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _securityProvider.AuthorizeProjectAccessAsync(_dbContext, "proj-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "user-id", DataConstants.ProjectRole.Viewer)));
        _repo.GetMembersAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(Enumerable.Empty<ProjectUserDbEntity>()));

        _controller = new GetProjectUsersController(_dbContextFactory, _repo, _securityProvider);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    [Test]
    public async Task GetProjectUsers_ReturnsUnauthorized_WhenCallerIsNotAuthorized()
    {
        _securityProvider.AuthorizeProjectAccessAsync(_dbContext, "proj-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure("User does not have access to this project.")));

        var result = await _controller.GetProjectUsers("proj-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetProjectUsers_ReturnsOk()
    {
        var result = await _controller.GetProjectUsers("proj-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetProjectUsers_ReturnsEmptyList_WhenProjectHasNoMembers()
    {
        var result = (OkObjectResult)await _controller.GetProjectUsers("proj-1", CancellationToken.None);
        var users  = result.Value as IEnumerable<GetProjectUsersResponse>;

        Assert.That(users, Is.Empty);
    }

    [Test]
    public async Task GetProjectUsers_ReturnsMappedUsers()
    {
        _repo.GetMembersAsync(_dbContext, "proj-1").Returns(Task.FromResult<IEnumerable<ProjectUserDbEntity>>(new[]
        {
            new ProjectUserDbEntity { UserId = "u1", Email = "a@b.com", DisplayName = "Alice", ProjectRole = DataConstants.ProjectRole.Admin },
            new ProjectUserDbEntity { UserId = "u2", Email = "c@d.com", DisplayName = "Carol", ProjectRole = DataConstants.ProjectRole.Viewer },
        }));

        var result = (OkObjectResult)await _controller.GetProjectUsers("proj-1", CancellationToken.None);
        var users  = (result.Value as IEnumerable<GetProjectUsersResponse>)!.ToList();

        Assert.That(users, Has.Count.EqualTo(2));
        Assert.That(users[0].UserId, Is.EqualTo("u1"));
        Assert.That(users[1].UserId, Is.EqualTo("u2"));
    }

    [Test]
    public async Task GetProjectUsers_QueriesMembersWithProjectId()
    {
        await _controller.GetProjectUsers("proj-1", CancellationToken.None);

        await _repo.Received(1).GetMembersAsync(_dbContext, "proj-1");
    }
}
