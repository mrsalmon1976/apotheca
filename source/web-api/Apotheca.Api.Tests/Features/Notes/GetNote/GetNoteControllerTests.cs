using System.Security.Claims;
using Apotheca.Api.Features.Notes.GetNote;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Notes.GetNote;

[TestFixture]
public class GetNoteControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private GetNoteRepository _repository = null!;
    private GetNoteController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _repository = Substitute.For<GetNoteRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new GetNoteController(_dbContextFactory, _repository);
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
    }

    private static GetNoteResponse ANote(string id = "note-1") => new()
    {
        Id        = id,
        Title     = "My Note",
        IsFolder  = false,
        Body      = "Some content",
    };

    // --- Identity ---

    [Test]
    public async Task GetNote_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.GetNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetNote_Returns401_WithErrorMessage_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = (UnauthorizedObjectResult)await _controller.GetNote("proj-1", "note-1", CancellationToken.None);
        var error = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    // --- Access control ---

    [Test]
    public async Task GetNote_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.GetNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetNote_ChecksAccessWithCorrectFirebaseUidAndProjectId()
    {
        SetAuthenticatedUser("uid-abc");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.GetNote("proj-xyz", "note-1", CancellationToken.None);

        await _repository.Received(1).UserHasProjectAccessAsync(_dbContext, "uid-abc", "proj-xyz");
    }

    [Test]
    public async Task GetNote_DoesNotQueryNote_WhenAccessDenied()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await _controller.GetNote("proj-1", "note-1", CancellationToken.None);

        await _repository.DidNotReceive().GetNoteAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Not found ---

    [Test]
    public async Task GetNote_Returns404_WhenNoteDoesNotExist()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetNoteResponse?>(null));

        var result = await _controller.GetNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetNote_QueriesWithCorrectProjectIdAndNoteId()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetNoteResponse?>(null));

        await _controller.GetNote("proj-xyz", "note-abc", CancellationToken.None);

        await _repository.Received(1).GetNoteAsync(_dbContext, "proj-xyz", "note-abc");
    }

    // --- Success ---

    [Test]
    public async Task GetNote_ReturnsOk_WhenNoteExists()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        _repository.GetNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetNoteResponse?>(ANote()));

        var result = await _controller.GetNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetNote_ReturnsMappedNote()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var note = new GetNoteResponse
        {
            Id           = "note-abc",
            ParentNoteId = "folder-id",
            IsFolder     = false,
            Title        = "Meeting Notes",
            Body         = "Item 1\nItem 2",
            Labels       = ["alpha", "beta"],
            CreatedAt    = new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero),
            UpdatedAt    = new DateTimeOffset(2025, 1, 16, 12, 0, 0, TimeSpan.Zero),
        };
        _repository.GetNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetNoteResponse?>(note));

        var result = (OkObjectResult)await _controller.GetNote("proj-1", "note-abc", CancellationToken.None);
        var response = result.Value as GetNoteResponse;

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Id, Is.EqualTo("note-abc"));
        Assert.That(response.ParentNoteId, Is.EqualTo("folder-id"));
        Assert.That(response.IsFolder, Is.False);
        Assert.That(response.Title, Is.EqualTo("Meeting Notes"));
        Assert.That(response.Body, Is.EqualTo("Item 1\nItem 2"));
        Assert.That(response.Labels, Is.EqualTo(new[] { "alpha", "beta" }));
        Assert.That(response.CreatedAt, Is.EqualTo(new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero)));
        Assert.That(response.UpdatedAt, Is.EqualTo(new DateTimeOffset(2025, 1, 16, 12, 0, 0, TimeSpan.Zero)));
    }

    [Test]
    public async Task GetNote_ReturnsMappedFolder()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();

        var folder = new GetNoteResponse
        {
            Id       = "folder-abc",
            IsFolder = true,
            Title    = "Sprint Notes",
        };
        _repository.GetNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetNoteResponse?>(folder));

        var result = (OkObjectResult)await _controller.GetNote("proj-1", "folder-abc", CancellationToken.None);
        var response = result.Value as GetNoteResponse;

        Assert.That(response!.IsFolder, Is.True);
        Assert.That(response.Body, Is.Null);
        Assert.That(response.ParentNoteId, Is.Null);
        Assert.That(response.Labels, Is.Empty);
    }
}
