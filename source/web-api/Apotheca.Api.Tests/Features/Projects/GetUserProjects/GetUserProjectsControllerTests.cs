using Apotheca.Api.Features.Projects.GetUserProjects;
using Apotheca.Api.Providers;
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
    private ISecurityProvider _securityProvider = null!;
    private GetUserProjectsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<GetUserProjectsRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _repository.GetProjectsByUidAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(Enumerable.Empty<ProjectDbEntity>()));
        _repository.GetProjectStatsAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(Enumerable.Empty<ProjectStatsModel>()));

        _controller = new GetUserProjectsController(_dbContextFactory, _repository, _securityProvider);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private void AllowAccess(string firebaseUid = "firebase-uid")
    {
        _securityProvider
            .AuthorizeAccessAsync(_dbContext, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success(firebaseUid, "user-id-123")));
    }

    private void DenyAccess(string errorMessage = "User identity could not be determined.")
    {
        _securityProvider
            .AuthorizeAccessAsync(_dbContext, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure(errorMessage)));
    }

    // --- Identity ---

    [Test]
    public async Task GetUserProjects_Returns401_WhenIdentityFails()
    {
        DenyAccess();

        var result = await _controller.GetUserProjects(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetUserProjects_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.GetUserProjects(CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Result shape ---

    [Test]
    public async Task GetUserProjects_ReturnsOk()
    {
        AllowAccess();

        var result = await _controller.GetUserProjects(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetUserProjects_ReturnsEmptyList_WhenUserHasNoProjects()
    {
        AllowAccess();

        var result   = (OkObjectResult)await _controller.GetUserProjects(CancellationToken.None);
        var projects = result.Value as IEnumerable<GetUserProjectsResponse>;

        Assert.That(projects, Is.Empty);
    }

    [Test]
    public async Task GetUserProjects_ReturnsMappedProjects()
    {
        AllowAccess();
        _repository.GetProjectsByUidAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<ProjectDbEntity>>(new[]
            {
                new ProjectDbEntity { Id = "p1", Name = "Alpha", CreatedAt = DateTimeOffset.UtcNow },
                new ProjectDbEntity { Id = "p2", Name = "Beta",  CreatedAt = DateTimeOffset.UtcNow },
            }));

        var result   = (OkObjectResult)await _controller.GetUserProjects(CancellationToken.None);
        var projects = (result.Value as IEnumerable<GetUserProjectsResponse>)!.ToList();

        Assert.That(projects, Has.Count.EqualTo(2));
        Assert.That(projects[0].Id, Is.EqualTo("p1"));
        Assert.That(projects[1].Id, Is.EqualTo("p2"));
    }

    // --- Queries ---

    [Test]
    public async Task GetUserProjects_QueriesProjectsWithFirebaseUid()
    {
        AllowAccess("firebase-uid");

        await _controller.GetUserProjects(CancellationToken.None);

        await _repository.Received(1).GetProjectsByUidAsync(_dbContext, "firebase-uid");
    }

    [Test]
    public async Task GetUserProjects_QueriesStatsWithFirebaseUid()
    {
        AllowAccess("firebase-uid");

        await _controller.GetUserProjects(CancellationToken.None);

        await _repository.Received(1).GetProjectStatsAsync(_dbContext, "firebase-uid");
    }

    // --- Stats ---

    [Test]
    public async Task GetUserProjects_MergesStatsIntoResponse()
    {
        AllowAccess();
        _repository.GetProjectsByUidAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<ProjectDbEntity>>(new[]
            {
                new ProjectDbEntity { Id = "p1", Name = "Alpha", ProjectRole = "owner", CreatedAt = DateTimeOffset.UtcNow },
            }));
        _repository.GetProjectStatsAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<ProjectStatsModel>>(new[]
            {
                new ProjectStatsModel { ProjectId = "p1", OpenTaskCount = 3, MemberCount = 2 },
            }));

        var result  = (OkObjectResult)await _controller.GetUserProjects(CancellationToken.None);
        var project = (result.Value as IEnumerable<GetUserProjectsResponse>)!.Single();

        Assert.That(project.ProjectRole,   Is.EqualTo("owner"));
        Assert.That(project.OpenTaskCount, Is.EqualTo(3));
        Assert.That(project.MemberCount,   Is.EqualTo(2));
    }

    [Test]
    public async Task GetUserProjects_DefaultsCountsToZero_WhenNoStatsForProject()
    {
        AllowAccess();
        _repository.GetProjectsByUidAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<ProjectDbEntity>>(new[]
            {
                new ProjectDbEntity { Id = "p1", Name = "Alpha", CreatedAt = DateTimeOffset.UtcNow },
            }));

        var result  = (OkObjectResult)await _controller.GetUserProjects(CancellationToken.None);
        var project = (result.Value as IEnumerable<GetUserProjectsResponse>)!.Single();

        Assert.That(project.OpenTaskCount, Is.EqualTo(0));
        Assert.That(project.MemberCount,   Is.EqualTo(0));
    }
}
