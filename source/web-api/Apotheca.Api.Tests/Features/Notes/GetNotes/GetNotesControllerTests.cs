using Apotheca.Api.Features.Notes.GetNotes;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Notes.GetNotes;

[TestFixture]
public class GetNotesControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private GetNotesRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private GetNotesController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<GetNotesRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _repository.GetNotesAsync(_dbContext, Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult(Enumerable.Empty<GetNotesResponse>()));

        _controller = new GetNotesController(_dbContextFactory, _repository, _securityProvider);
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
    public async Task GetNotes_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.GetNotes("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetNotes_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.GetNotes("proj-1", null, CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task GetNotes_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.GetNotes("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetNotes_DoesNotQueryNotes_WhenAccessDenied()
    {
        DenyProjectAccess();

        await _controller.GetNotes("proj-1", null, CancellationToken.None);

        await _repository.DidNotReceive().GetNotesAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string?>());
    }

    // --- parentId routing ---

    [Test]
    public async Task GetNotes_CallsRepository_WithNullParentId_WhenNoParentIdProvided()
    {
        AllowProjectAccess();

        await _controller.GetNotes("proj-1", null, CancellationToken.None);

        await _repository.Received(1).GetNotesAsync(_dbContext, "proj-1", null);
    }

    [Test]
    public async Task GetNotes_CallsRepository_WithParentId_WhenParentIdProvided()
    {
        AllowProjectAccess();

        await _controller.GetNotes("proj-1", "folder-id-abc", CancellationToken.None);

        await _repository.Received(1).GetNotesAsync(_dbContext, "proj-1", "folder-id-abc");
    }

    // --- Result shape ---

    [Test]
    public async Task GetNotes_ReturnsOk()
    {
        AllowProjectAccess();

        var result = await _controller.GetNotes("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetNotes_ReturnsEmptyList_WhenNoNotesExist()
    {
        AllowProjectAccess();

        var result = (OkObjectResult)await _controller.GetNotes("proj-1", null, CancellationToken.None);
        var notes  = result.Value as IEnumerable<GetNotesResponse>;

        Assert.That(notes, Is.Empty);
    }

    [Test]
    public async Task GetNotes_ReturnsMappedNotes()
    {
        AllowProjectAccess();

        var repoResults = new[]
        {
            new GetNotesResponse { Id = "n1", Title = "Folder A", IsFolder = true },
            new GetNotesResponse { Id = "n2", Title = "A note",   IsFolder = false },
        };
        _repository.GetNotesAsync(_dbContext, Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult<IEnumerable<GetNotesResponse>>(repoResults));

        var result = (OkObjectResult)await _controller.GetNotes("proj-1", null, CancellationToken.None);
        var notes  = (result.Value as IEnumerable<GetNotesResponse>)!.ToList();

        Assert.That(notes,           Has.Count.EqualTo(2));
        Assert.That(notes[0].Id,     Is.EqualTo("n1"));
        Assert.That(notes[0].IsFolder, Is.True);
        Assert.That(notes[1].Id,     Is.EqualTo("n2"));
        Assert.That(notes[1].IsFolder, Is.False);
    }

    [Test]
    public async Task GetNotes_MapsParentNoteId_InResponse()
    {
        AllowProjectAccess();

        var repoResults = new[]
        {
            new GetNotesResponse { Id = "n1", Title = "Child note", IsFolder = false, ParentNoteId = "folder-id-abc" },
        };
        _repository.GetNotesAsync(_dbContext, Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult<IEnumerable<GetNotesResponse>>(repoResults));

        var result = (OkObjectResult)await _controller.GetNotes("proj-1", "folder-id-abc", CancellationToken.None);
        var notes  = (result.Value as IEnumerable<GetNotesResponse>)!.ToList();

        Assert.That(notes[0].ParentNoteId, Is.EqualTo("folder-id-abc"));
    }
}
