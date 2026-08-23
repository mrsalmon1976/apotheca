using Apotheca.Api.Features.Workspaces.CreateWorkspace;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Workspaces.CreateWorkspace;

[TestFixture]
public class CreateWorkspaceControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private CreateWorkspaceRepository _repo = null!;
    private CreateWorkspaceValidator _validator = null!;
    private ISecurityProvider _securityProvider = null!;
    private CreateWorkspaceController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repo = Substitute.For<CreateWorkspaceRepository>();
        _validator = Substitute.For<CreateWorkspaceValidator>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _securityProvider.AuthorizeAccessAsync(_dbContext, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("fb-uid", "user-id-123")));
        _repo.CreateWorkspaceAsync(_dbContext, Arg.Any<string>()).Returns(Task.FromResult("ws-1"));

        _controller = new CreateWorkspaceController(_dbContextFactory, _repo, _validator, _securityProvider);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private static CreateWorkspaceRequest ValidRequest(string name = "Acme Corp") => new() { Name = name };

    [Test]
    public async Task CreateWorkspace_ReturnsBadRequest_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<CreateWorkspaceRequest>()).Returns(new[] { "Name is required." });

        var result = await _controller.CreateWorkspace(ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateWorkspace_ReturnsUnauthorized_WhenCallerIsNotAuthorized()
    {
        _securityProvider.AuthorizeAccessAsync(_dbContext, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure("User identity could not be determined.")));

        var result = await _controller.CreateWorkspace(ValidRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task CreateWorkspace_ReturnsOk_WithWorkspaceIdAndName()
    {
        var result = (OkObjectResult)await _controller.CreateWorkspace(ValidRequest("Acme Corp"), CancellationToken.None);
        var id     = result.Value?.GetType().GetProperty("id")?.GetValue(result.Value)?.ToString();
        var name   = result.Value?.GetType().GetProperty("name")?.GetValue(result.Value)?.ToString();

        Assert.That(id, Is.EqualTo("ws-1"));
        Assert.That(name, Is.EqualTo("Acme Corp"));
    }

    [Test]
    public async Task CreateWorkspace_TrimsName_BeforeCreating()
    {
        await _controller.CreateWorkspace(ValidRequest("  Acme Corp  "), CancellationToken.None);

        await _repo.Received(1).CreateWorkspaceAsync(_dbContext, "Acme Corp");
    }

    [Test]
    public async Task CreateWorkspace_AddsCreatorAsAdminMember()
    {
        await _controller.CreateWorkspace(ValidRequest(), CancellationToken.None);

        await _repo.Received(1).CreateWorkspaceMemberAsync(_dbContext, "ws-1", "user-id-123", DataConstants.WorkspaceRole.Admin);
    }

    [Test]
    public async Task CreateWorkspace_SetsCreatedWorkspaceAsCurrent()
    {
        await _controller.CreateWorkspace(ValidRequest(), CancellationToken.None);

        await _repo.Received(1).SetCurrentWorkspaceAsync(_dbContext, "user-id-123", "ws-1");
    }
}
