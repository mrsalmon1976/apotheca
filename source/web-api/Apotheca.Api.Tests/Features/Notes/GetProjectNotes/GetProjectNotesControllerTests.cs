using Apotheca.Api.Features.Notes.GetProjectNotes;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Notes.GetProjectNotes;

[TestFixture]
public class GetProjectNotesControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private GetProjectNotesRepository _repository = null!;
    private ISecurityProvider _securityProvider = null!;
    private GetProjectNotesController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<GetProjectNotesRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new GetProjectNotesController(_dbContextFactory, _repository, _securityProvider);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private static IEnumerable<ProjectNoteModel> EmptyNotes() => Enumerable.Empty<ProjectNoteModel>();

    private void AllowProjectAccess(string firebaseUid = "firebase-uid")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success(firebaseUid, "user-id-123")));
    }

    private void DenyProjectAccess(string errorMessage = "User does not have access to this project.")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure(errorMessage)));
    }

    // --- Identity / Access control ---

    [Test]
    public async Task GetRecentNotes_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.GetRecentNotes("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetRecentNotes_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.GetRecentNotes("proj-1", null, CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task GetRecentNotes_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.GetRecentNotes("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetRecentNotes_DoesNotQueryNotes_WhenAccessDenied()
    {
        DenyProjectAccess();

        await _controller.GetRecentNotes("proj-1", null, CancellationToken.None);

        await _repository.DidNotReceive().GetRecentNotesAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<int?>());
    }

    // --- Result shape ---

    [Test]
    public async Task GetRecentNotes_ReturnsOk()
    {
        AllowProjectAccess();
        _repository.GetRecentNotesAsync(_dbContext, Arg.Any<string>(), Arg.Any<int?>())
            .Returns(Task.FromResult(EmptyNotes()));

        var result = await _controller.GetRecentNotes("proj-1", null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetRecentNotes_ReturnsEmptyList_WhenNoNotesExist()
    {
        AllowProjectAccess();
        _repository.GetRecentNotesAsync(_dbContext, Arg.Any<string>(), Arg.Any<int?>())
            .Returns(Task.FromResult(EmptyNotes()));

        var result = (OkObjectResult)await _controller.GetRecentNotes("proj-1", null, CancellationToken.None);
        var notes  = result.Value as IEnumerable<GetProjectNotesResponse>;

        Assert.That(notes, Is.Empty);
    }

    // --- Passthrough of identifiers ---

    [Test]
    public async Task GetRecentNotes_PassesProjectIdToRepository()
    {
        AllowProjectAccess();
        _repository.GetRecentNotesAsync(_dbContext, Arg.Any<string>(), Arg.Any<int?>())
            .Returns(Task.FromResult(EmptyNotes()));

        await _controller.GetRecentNotes("proj-xyz", null, CancellationToken.None);

        await _repository.Received(1).GetRecentNotesAsync(_dbContext, "proj-xyz", null);
    }

    [Test]
    public async Task GetRecentNotes_PassesLimitToRepository()
    {
        AllowProjectAccess();
        _repository.GetRecentNotesAsync(_dbContext, Arg.Any<string>(), Arg.Any<int?>())
            .Returns(Task.FromResult(EmptyNotes()));

        await _controller.GetRecentNotes("proj-1", 10, CancellationToken.None);

        await _repository.Received(1).GetRecentNotesAsync(_dbContext, Arg.Any<string>(), 10);
    }

    // --- Mapped response ---

    [Test]
    public async Task GetRecentNotes_ReturnsMappedNotes()
    {
        AllowProjectAccess();

        var dbResults = new[]
        {
            new ProjectNoteModel { Id = "n1", Title = "First note",  CreatedBy = "user-1", UpdatedAt = DateTimeOffset.UtcNow },
            new ProjectNoteModel { Id = "n2", Title = "Second note", CreatedBy = "user-1", UpdatedAt = DateTimeOffset.UtcNow },
        };
        _repository.GetRecentNotesAsync(_dbContext, Arg.Any<string>(), Arg.Any<int?>())
            .Returns(Task.FromResult<IEnumerable<ProjectNoteModel>>(dbResults));

        var result = (OkObjectResult)await _controller.GetRecentNotes("proj-1", null, CancellationToken.None);
        var notes  = (result.Value as IEnumerable<GetProjectNotesResponse>)!.ToList();

        Assert.That(notes, Has.Count.EqualTo(2));
        Assert.That(notes[0].Id, Is.EqualTo("n1"));
        Assert.That(notes[1].Id, Is.EqualTo("n2"));
    }
}
