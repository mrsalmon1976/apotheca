using Apotheca.Api.Features.Notes.SaveNote;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Notes.SaveNote;

[TestFixture]
public class SaveNoteControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private SaveNoteRepository _repository = null!;
    private SaveNoteValidator _validator = null!;
    private ISecurityProvider _securityProvider = null!;
    private SaveNoteController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<SaveNoteRepository>();
        _validator        = Substitute.For<SaveNoteValidator>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _validator.Validate(Arg.Any<SaveNoteRequest>()).Returns([]);
        _repository.GetNoteTitleBodyAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult(("Note Title", "Note body")));

        _controller = new SaveNoteController(_dbContextFactory, _repository, _validator, _securityProvider);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private void AllowProjectAccess(string userId = "user-id-123")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("firebase-uid", userId)));
    }

    private void DenyProjectAccess(string errorMessage = "User does not have access to this project.")
    {
        _securityProvider
            .AuthorizeProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure(errorMessage)));
    }

    private void NoteExists(bool exists = true)
    {
        _repository.NoteExistsAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(exists));
    }

    // --- Validation ---

    [Test]
    public async Task SaveNote_Returns400_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveNoteRequest>()).Returns(["At least one field must be provided."]);

        var result = await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SaveNote_DoesNotOpenDatabase_WhenValidationFails()
    {
        _validator.Validate(Arg.Any<SaveNoteRequest>()).Returns(["At least one field must be provided."]);

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest(), CancellationToken.None);

        await _dbContextFactory.DidNotReceive().CreateAsync(Arg.Any<CancellationToken>());
    }

    // --- Identity / Access control ---

    [Test]
    public async Task SaveNote_Returns401_WhenIdentityFails()
    {
        DenyProjectAccess("User identity could not be determined.");

        var result = await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "Title" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SaveNote_Returns401_WhenProjectAccessDenied()
    {
        DenyProjectAccess();

        var result = await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "Title" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Note existence ---

    [Test]
    public async Task SaveNote_Returns404_WhenNoteDoesNotExist()
    {
        AllowProjectAccess();
        NoteExists(false);

        var result = await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "Title" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // --- Success ---

    [Test]
    public async Task SaveNote_Returns200_WhenNoteIsUpdated()
    {
        AllowProjectAccess();
        NoteExists();

        var result = await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "New Title" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    // --- Title/Body update ---

    [Test]
    public async Task SaveNote_CallsUpdateNoteCore_WhenTitleIsProvided()
    {
        AllowProjectAccess();
        NoteExists();

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "New Title" }, CancellationToken.None);

        await _repository.Received(1).UpdateNoteCoreAsync(_dbContext, "proj-1", "note-1", "New Title", null);
    }

    [Test]
    public async Task SaveNote_CallsUpdateNoteCore_WhenBodyIsProvided()
    {
        AllowProjectAccess();
        NoteExists();

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Body = "Some content" }, CancellationToken.None);

        await _repository.Received(1).UpdateNoteCoreAsync(_dbContext, "proj-1", "note-1", null, "Some content");
    }

    [Test]
    public async Task SaveNote_TrimsTitle_BeforeCallingRepository()
    {
        AllowProjectAccess();
        NoteExists();

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "  My Note  " }, CancellationToken.None);

        await _repository.Received(1).UpdateNoteCoreAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), "My Note", Arg.Any<string?>());
    }

    [Test]
    public async Task SaveNote_DoesNotCallUpdateNoteCore_WhenOnlyLabelsAreProvided()
    {
        AllowProjectAccess();
        NoteExists();

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Labels = ["tag"] }, CancellationToken.None);

        await _repository.DidNotReceive().UpdateNoteCoreAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>());
    }

    // --- Label sync ---

    [Test]
    public async Task SaveNote_DeletesAndResyncsLabels_WhenLabelsAreProvided()
    {
        AllowProjectAccess();
        NoteExists();
        _repository.UpsertLabelAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult("label-id"));

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Labels = ["tag1"] }, CancellationToken.None);

        await _repository.Received(1).DeleteNoteLabelsAsync(_dbContext, "note-1");
        await _repository.Received(1).UpsertLabelAsync(_dbContext, "proj-1", Arg.Any<string>(), "tag1");
        await _repository.Received(1).InsertNoteLabelAsync(_dbContext, "note-1", "label-id");
    }

    [Test]
    public async Task SaveNote_DoesNotSyncLabels_WhenLabelsAreNull()
    {
        AllowProjectAccess();
        NoteExists();

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "Title" }, CancellationToken.None);

        await _repository.DidNotReceive().DeleteNoteLabelsAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    [Test]
    public async Task SaveNote_ClearsAllLabels_WhenEmptyLabelsListIsProvided()
    {
        AllowProjectAccess();
        NoteExists();

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Labels = [] }, CancellationToken.None);

        await _repository.Received(1).DeleteNoteLabelsAsync(_dbContext, "note-1");
        await _repository.DidNotReceive().UpsertLabelAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Search ---

    [Test]
    public async Task SaveNote_UpsertsSearchRecord_UsingCurrentTitleAndBody()
    {
        AllowProjectAccess();
        NoteExists();
        _repository.GetNoteTitleBodyAsync(_dbContext, "note-1")
            .Returns(Task.FromResult(("My Note", "Some content")));

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "My Note" }, CancellationToken.None);

        await _repository.Received(1).UpsertSearchAsync(_dbContext, "proj-1", "note-1", "My Note", "Some content");
    }

    [Test]
    public async Task SaveNote_UpsertsSearchRecord_WhenOnlyLabelsAreUpdated()
    {
        AllowProjectAccess();
        NoteExists();
        _repository.UpsertLabelAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult("label-id"));
        _repository.GetNoteTitleBodyAsync(_dbContext, "note-1")
            .Returns(Task.FromResult(("Existing Title", "Existing body")));

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Labels = ["tag"] }, CancellationToken.None);

        await _repository.Received(1).UpsertSearchAsync(_dbContext, "proj-1", "note-1", "Existing Title", "Existing body");
    }

    [Test]
    public async Task SaveNote_DoesNotUpsertSearchRecord_WhenAccessIsDenied()
    {
        DenyProjectAccess();

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "Title" }, CancellationToken.None);

        await _repository.DidNotReceive().UpsertSearchAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task SaveNote_DoesNotUpsertSearchRecord_WhenNoteDoesNotExist()
    {
        AllowProjectAccess();
        NoteExists(false);

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "Title" }, CancellationToken.None);

        await _repository.DidNotReceive().UpsertSearchAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
