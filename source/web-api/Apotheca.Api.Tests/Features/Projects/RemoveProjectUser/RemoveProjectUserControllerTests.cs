using Apotheca.Api.Features.Projects.RemoveProjectUser;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Projects.RemoveProjectUser;

[TestFixture]
public class RemoveProjectUserControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private RemoveProjectUserRepository _repo = null!;
    private ISecurityProvider _securityProvider = null!;
    private RemoveProjectUserController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repo = Substitute.For<RemoveProjectUserRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _securityProvider.AuthorizeProjectAccessAsync(_dbContext, "proj-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "admin-user-id", DataConstants.ProjectRole.Admin)));

        _controller = new RemoveProjectUserController(_dbContextFactory, _repo, _securityProvider);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    [Test]
    public async Task RemoveProjectUser_ReturnsBadRequest_WhenRemovingLastAdmin()
    {
        _repo.GetMemberRoleAsync(_dbContext, "proj-1", "u2").Returns(Task.FromResult<string?>(DataConstants.ProjectRole.Admin));
        _repo.CountAdminsAsync(_dbContext, "proj-1").Returns(Task.FromResult(1));

        var result = await _controller.RemoveProjectUser("proj-1", "u2", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RemoveProjectUser_Succeeds_WhenRemovingContributor()
    {
        _repo.GetMemberRoleAsync(_dbContext, "proj-1", "u2").Returns(Task.FromResult<string?>(DataConstants.ProjectRole.Contributor));

        var result = await _controller.RemoveProjectUser("proj-1", "u2", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
        await _repo.Received(1).RemoveMemberAsync(_dbContext, "proj-1", "u2");
    }

    [Test]
    public async Task RemoveProjectUser_ReturnsUnauthorized_WhenCallerIsNotProjectAdmin()
    {
        _securityProvider.AuthorizeProjectAccessAsync(_dbContext, "proj-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "user-id", DataConstants.ProjectRole.Viewer)));

        var result = await _controller.RemoveProjectUser("proj-1", "u2", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }
}
