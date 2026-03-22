using System.Security.Claims;
using Apotheca.Api.Features.Projects.GetUserProjects;
using Apotheca.Data;
using Apotheca.Data.DbEntities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Projects.GetUserProjects;

[TestFixture]
public class GetUserProjectsControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private GetUserProjectsRepository _repository = null!;
    private GetUserProjectsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repository = Substitute.For<GetUserProjectsRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new GetUserProjectsController(_dbContextFactory, _repository);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    private void SetAuthenticatedUser(string firebaseUid)
    {
        var claims = new[] { new Claim("sub", firebaseUid) };
        var identity = new ClaimsIdentity(claims, "test");
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
    }

    // --- Identity ---

    [Test]
    public async Task GetUserProjects_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.GetUserProjects(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetUserProjects_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.GetUserProjects(CancellationToken.None);
        var error = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Result shape ---

    [Test]
    public async Task GetUserProjects_ReturnsOk()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.GetProjectsByUidAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(Enumerable.Empty<ProjectDbEntity>()));

        var result = await _controller.GetUserProjects(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetUserProjects_ReturnsEmptyList_WhenUserHasNoProjects()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.GetProjectsByUidAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(Enumerable.Empty<ProjectDbEntity>()));

        var result = (OkObjectResult)await _controller.GetUserProjects(CancellationToken.None);
        var projects = result.Value as IEnumerable<GetUserProjectsResponse>;

        Assert.That(projects, Is.Empty);
    }

    [Test]
    public async Task GetUserProjects_QueriesWithFirebaseUid()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.GetProjectsByUidAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(Enumerable.Empty<ProjectDbEntity>()));

        await _controller.GetUserProjects(CancellationToken.None);

        await _repository.Received(1).GetProjectsByUidAsync(_dbContext, "firebase-uid");
    }

    [Test]
    public async Task GetUserProjects_ReturnsMappedProjects()
    {
        SetAuthenticatedUser("firebase-uid");

        var dbResults = new[]
        {
            new ProjectDbEntity { Id = "p1", Name = "Alpha", CreatedAt = DateTimeOffset.UtcNow },
            new ProjectDbEntity { Id = "p2", Name = "Beta",  CreatedAt = DateTimeOffset.UtcNow },
        };
        _repository.GetProjectsByUidAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<ProjectDbEntity>>(dbResults));

        var result = (OkObjectResult)await _controller.GetUserProjects(CancellationToken.None);
        var projects = (result.Value as IEnumerable<GetUserProjectsResponse>)!.ToList();

        Assert.That(projects, Has.Count.EqualTo(2));
        Assert.That(projects[0].Id, Is.EqualTo("p1"));
        Assert.That(projects[1].Id, Is.EqualTo("p2"));
    }
}
