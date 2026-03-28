using System.Security.Claims;
using Apotheca.Api.Features.Notes.SaveNote;
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
    private SaveNoteController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<SaveNoteRepository>();
        _validator        = Substitute.For<SaveNoteValidator>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));
        _validator.Validate(Arg.Any<SaveNoteRequest>()).Returns([]);

        _controller = new SaveNoteController(_dbContextFactory, _repository, _validator);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    private void SetAuthenticatedUser(string firebaseUid)
    {
        var claims   = new[] { new Claim("sub", firebaseUid) };
        var identity = new ClaimsIdentity(claims, "test");
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
    }

    private void AllowProjectAccess(string userId = "user-id-123")
    {
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        _repository.GetUserIdAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<string?>(userId));
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

    // --- Identity ---

    [Test]
    public async Task SaveNote_Returns401_WhenSubClaimIsMissing()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "Title" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Access control ---

    [Test]
    public async Task SaveNote_Returns403_WhenUserDoesNotHaveProjectAccess()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "Title" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    // --- User lookup ---

    [Test]
    public async Task SaveNote_Returns401_WhenUserIdCannotBeResolved()
    {
        SetAuthenticatedUser("firebase-uid");
        _repository.UserHasProjectAccessAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        _repository.GetUserIdAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<string?>(null));

        var result = await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "Title" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // --- Note existence ---

    [Test]
    public async Task SaveNote_Returns404_WhenNoteDoesNotExist()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        NoteExists(false);

        var result = await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "Title" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // --- Success ---

    [Test]
    public async Task SaveNote_Returns200_WhenNoteIsUpdated()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        NoteExists();

        var result = await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "New Title" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    // --- Title/Body update ---

    [Test]
    public async Task SaveNote_CallsUpdateNoteCore_WhenTitleIsProvided()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        NoteExists();

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "New Title" }, CancellationToken.None);

        await _repository.Received(1).UpdateNoteCoreAsync(_dbContext, "proj-1", "note-1", "New Title", null);
    }

    [Test]
    public async Task SaveNote_CallsUpdateNoteCore_WhenBodyIsProvided()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        NoteExists();

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Body = "Some content" }, CancellationToken.None);

        await _repository.Received(1).UpdateNoteCoreAsync(_dbContext, "proj-1", "note-1", null, "Some content");
    }

    [Test]
    public async Task SaveNote_TrimsTitle_BeforeCallingRepository()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        NoteExists();

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "  My Note  " }, CancellationToken.None);

        await _repository.Received(1).UpdateNoteCoreAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), "My Note", Arg.Any<string?>());
    }

    [Test]
    public async Task SaveNote_DoesNotCallUpdateNoteCore_WhenOnlyLabelsAreProvided()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        NoteExists();

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Labels = ["tag"] }, CancellationToken.None);

        await _repository.DidNotReceive().UpdateNoteCoreAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>());
    }

    // --- Label sync ---

    [Test]
    public async Task SaveNote_DeletesAndResyncsLabels_WhenLabelsAreProvided()
    {
        SetAuthenticatedUser("firebase-uid");
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
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        NoteExists();

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Title = "Title" }, CancellationToken.None);

        await _repository.DidNotReceive().DeleteNoteLabelsAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    [Test]
    public async Task SaveNote_ClearsAllLabels_WhenEmptyLabelsListIsProvided()
    {
        SetAuthenticatedUser("firebase-uid");
        AllowProjectAccess();
        NoteExists();

        await _controller.SaveNote("proj-1", "note-1", new SaveNoteRequest { Labels = [] }, CancellationToken.None);

        await _repository.Received(1).DeleteNoteLabelsAsync(_dbContext, "note-1");
        await _repository.DidNotReceive().UpsertLabelAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
