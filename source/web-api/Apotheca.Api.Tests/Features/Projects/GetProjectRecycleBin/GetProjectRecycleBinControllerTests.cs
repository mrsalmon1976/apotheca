using Apotheca.Api.Features.Projects.GetProjectRecycleBin;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Projects.GetProjectRecycleBin;

[TestFixture]
public class GetProjectRecycleBinControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private GetProjectRecycleBinRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private GetProjectRecycleBinController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<GetProjectRecycleBinRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _repository.GetDeletedNotesAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(Enumerable.Empty<GetProjectRecycleBinResponse>()));

        _controller = new GetProjectRecycleBinController(_dbContextFactory, _repository, _securityProvider);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private void AllowProjectAccess()
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("firebase-uid", "user-id-123")));
    }

    private void DenyProjectAccess(string errorMessage = "User does not have access to this project.")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure(errorMessage)));
    }

    // --- Identity / Access control ---

    [Test]
    public async Task GetProjectRecycleBin_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.GetProjectRecycleBin("proj-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetProjectRecycleBin_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.GetProjectRecycleBin("proj-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task GetProjectRecycleBin_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.GetProjectRecycleBin("proj-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Result shape ---

    [Test]
    public async Task GetProjectRecycleBin_ReturnsOk_WhenUserHasAccess()
    {
        AllowProjectAccess();

        var result = await _controller.GetProjectRecycleBin("proj-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetProjectRecycleBin_ReturnsEmptyList_WhenNoDeletedNotesExist()
    {
        AllowProjectAccess();

        var result  = (OkObjectResult)await _controller.GetProjectRecycleBin("proj-1", CancellationToken.None);
        var entries = result.Value as IEnumerable<GetProjectRecycleBinResponse>;

        Assert.That(entries, Is.Empty);
    }

    [Test]
    public async Task GetProjectRecycleBin_QueriesWithCorrectProjectId()
    {
        AllowProjectAccess();

        await _controller.GetProjectRecycleBin("proj-xyz", CancellationToken.None);

        await _repository.Received(1).GetDeletedNotesAsync(_dbContext, "proj-xyz");
    }

    [Test]
    public async Task GetProjectRecycleBin_ReturnsDeletedNotes()
    {
        AllowProjectAccess();

        var now = DateTimeOffset.UtcNow;
        var dbResults = new[]
        {
            new GetProjectRecycleBinResponse { Id = "note-aaa", Type = "NOTE",   Title = "My Note",   DeletedBy = "Alice", DeletedAt = now },
            new GetProjectRecycleBinResponse { Id = "note-bbb", Type = "FOLDER", Title = "My Folder", DeletedBy = "Bob",   DeletedAt = now },
        };
        _repository.GetDeletedNotesAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<GetProjectRecycleBinResponse>>(dbResults));

        var result  = (OkObjectResult)await _controller.GetProjectRecycleBin("proj-1", CancellationToken.None);
        var entries = (result.Value as IEnumerable<GetProjectRecycleBinResponse>)!.ToList();

        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.That(entries[0].Id,    Is.EqualTo("note-aaa"));
        Assert.That(entries[0].Type,  Is.EqualTo("NOTE"));
        Assert.That(entries[0].Title, Is.EqualTo("My Note"));
        Assert.That(entries[1].Type,  Is.EqualTo("FOLDER"));
    }
}
