using Apotheca.Api.Features.Notes.GetNote;
using Apotheca.Api.Providers;
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
    private ISecurityProvider _securityProvider = null!;
    private GetNoteController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<GetNoteRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new GetNoteController(_dbContextFactory, _repository, _securityProvider);
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

    private static GetNoteResponse ANote(string id = "note-1") => new()
    {
        Id       = id,
        Title    = "My Note",
        IsFolder = false,
        Body     = "Some content",
    };

    // --- Identity / Access control ---

    [Test]
    public async Task GetNote_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.GetNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetNote_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.GetNote("proj-1", "note-1", CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task GetNote_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.GetNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetNote_ChecksAccessWithCorrectProjectId()
    {
        DenyProjectAccess();

        await _controller.GetNote("proj-xyz", "note-1", CancellationToken.None);

        await _securityProvider.Received(1).AuthorizeProjectAccessAsync(_dbContext, "proj-xyz", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetNote_DoesNotQueryNote_WhenAccessDenied()
    {
        DenyProjectAccess();

        await _controller.GetNote("proj-1", "note-1", CancellationToken.None);

        await _repository.DidNotReceive().GetNoteAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Not found ---

    [Test]
    public async Task GetNote_Returns404_WhenNoteDoesNotExist()
    {
        AllowProjectAccess();
        _repository.GetNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetNoteResponse?>(null));

        var result = await _controller.GetNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetNote_QueriesWithCorrectProjectIdAndNoteId()
    {
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
        AllowProjectAccess();
        _repository.GetNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetNoteResponse?>(ANote()));

        var result = await _controller.GetNote("proj-1", "note-1", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetNote_ReturnsMappedNote()
    {
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

        var result   = (OkObjectResult)await _controller.GetNote("proj-1", "note-abc", CancellationToken.None);
        var response = result.Value as GetNoteResponse;

        Assert.That(response,                 Is.Not.Null);
        Assert.That(response!.Id,             Is.EqualTo("note-abc"));
        Assert.That(response.ParentNoteId,    Is.EqualTo("folder-id"));
        Assert.That(response.IsFolder,        Is.False);
        Assert.That(response.Title,           Is.EqualTo("Meeting Notes"));
        Assert.That(response.Body,            Is.EqualTo("Item 1\nItem 2"));
        Assert.That(response.Labels,          Is.EqualTo(new[] { "alpha", "beta" }));
        Assert.That(response.CreatedAt,       Is.EqualTo(new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero)));
        Assert.That(response.UpdatedAt,       Is.EqualTo(new DateTimeOffset(2025, 1, 16, 12, 0, 0, TimeSpan.Zero)));
    }

    [Test]
    public async Task GetNote_ReturnsMappedFolder()
    {
        AllowProjectAccess();

        var folder = new GetNoteResponse { Id = "folder-abc", IsFolder = true, Title = "Sprint Notes" };
        _repository.GetNoteAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<GetNoteResponse?>(folder));

        var result   = (OkObjectResult)await _controller.GetNote("proj-1", "folder-abc", CancellationToken.None);
        var response = result.Value as GetNoteResponse;

        Assert.That(response!.IsFolder,        Is.True);
        Assert.That(response.Body,             Is.Null);
        Assert.That(response.ParentNoteId,     Is.Null);
        Assert.That(response.Labels,           Is.Empty);
    }
}
