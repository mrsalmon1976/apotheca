using System.Security.Claims;
using Apotheca.Api.Features.Notes.GetNotes;
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
    private GetNotesController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repository = Substitute.For<GetNotesRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new GetNotesController(_dbContextFactory, _repository);
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

    private void AllowProjectAccess()
    {
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        _repository.GetNotesAsync(_dbContext, Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult(Enumerable.Empty<GetNotesResponse>()));
    }

    // --- Identity ---

    [Test]
    public async Task GetNotes_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.GetNotes("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetNotes_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.GetNotes("proj-1", null, CancellationToken.None);
        var error = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task GetNotes_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.GetNotes("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetNotes_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.GetNotes("proj-xyz", null, CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    [Test]
    public async Task GetNotes_DoesNotQueryNotes_WhenAccessDenied()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.GetNotes("proj-1", null, CancellationToken.None);

        await _repository.DidNotReceive().GetNotesAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string?>());
    }

    // --- parentId routing ---

    [Test]
    public async Task GetNotes_CallsRepository_WithNullParentId_WhenNoParentIdProvided()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        await _controller.GetNotes("proj-1", null, CancellationToken.None);

        await _repository.Received(1).GetNotesAsync(_dbContext, "proj-1", null);
    }

    [Test]
    public async Task GetNotes_CallsRepository_WithParentId_WhenParentIdProvided()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        await _controller.GetNotes("proj-1", "folder-id-abc", CancellationToken.None);

        await _repository.Received(1).GetNotesAsync(_dbContext, "proj-1", "folder-id-abc");
    }

    // --- Result shape ---

    [Test]
    public async Task GetNotes_ReturnsOk()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var result = await _controller.GetNotes("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetNotes_ReturnsEmptyList_WhenNoNotesExist()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var result = (OkObjectResult)await _controller.GetNotes("proj-1", null, CancellationToken.None);
        var notes = result.Value as IEnumerable<GetNotesResponse>;

        Assert.That(notes, Is.Empty);
    }

    [Test]
    public async Task GetNotes_ReturnsMappedNotes()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        var repoResults = new[]
        {
            new GetNotesResponse { Id = "n1", Title = "Folder A", IsFolder = true },
            new GetNotesResponse { Id = "n2", Title = "A note",   IsFolder = false },
        };
        _repository.GetNotesAsync(_dbContext, Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult<IEnumerable<GetNotesResponse>>(repoResults));

        var result = (OkObjectResult)await _controller.GetNotes("proj-1", null, CancellationToken.None);
        var notes = (result.Value as IEnumerable<GetNotesResponse>)!.ToList();

        Assert.That(notes, Has.Count.EqualTo(2));
        Assert.That(notes[0].Id, Is.EqualTo("n1"));
        Assert.That(notes[0].IsFolder, Is.True);
        Assert.That(notes[1].Id, Is.EqualTo("n2"));
        Assert.That(notes[1].IsFolder, Is.False);
    }

    [Test]
    public async Task GetNotes_MapsParentNoteId_InResponse()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        var repoResults = new[]
        {
            new GetNotesResponse { Id = "n1", Title = "Child note", IsFolder = false, ParentNoteId = "folder-id-abc" },
        };
        _repository.GetNotesAsync(_dbContext, Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult<IEnumerable<GetNotesResponse>>(repoResults));

        var result = (OkObjectResult)await _controller.GetNotes("proj-1", "folder-id-abc", CancellationToken.None);
        var notes = (result.Value as IEnumerable<GetNotesResponse>)!.ToList();

        Assert.That(notes[0].ParentNoteId, Is.EqualTo("folder-id-abc"));
    }
}
