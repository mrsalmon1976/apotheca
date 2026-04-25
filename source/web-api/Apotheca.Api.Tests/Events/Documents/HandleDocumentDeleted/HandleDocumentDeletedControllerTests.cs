using System.Text;
using System.Text.Json;
using Apotheca.Api.Events;
using Apotheca.Api.Events.Documents;
using Apotheca.Api.Events.Documents.HandleDocumentDeleted;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Events.Documents.HandleDocumentDeleted;

[TestFixture]
public class HandleDocumentDeletedControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private HandleDocumentDeletedRepository _repository = null!;
    private HandleDocumentDeletedController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<HandleDocumentDeletedRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _repository.SoftDeleteDescendantsAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IReadOnlyList<DeletedDescendant>>([]));

        _controller = new HandleDocumentDeletedController(
            _dbContextFactory, _repository, Substitute.For<ILogger<HandleDocumentDeletedController>>());
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private static PubSubPushRequest BuildRequest(DocumentDeletedEvent eventData)
    {
        var json    = JsonSerializer.Serialize(eventData);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return new PubSubPushRequest
        {
            Message      = new PubSubMessage { Data = encoded },
            Subscription = "projects/test/subscriptions/document-deleted-sub",
        };
    }

    // --- Deserialization ---

    [Test]
    public async Task Handle_Returns400_WhenMessageDataIsEmpty()
    {
        var request = new PubSubPushRequest
        {
            Message      = new PubSubMessage { Data = string.Empty },
            Subscription = "sub",
        };

        var result = await _controller.Handle(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestResult>());
    }

    // --- Non-folder short circuit ---

    [Test]
    public async Task Handle_Returns204_WhenEventIsForDocument_NotFolder()
    {
        var request = BuildRequest(new DocumentDeletedEvent
        {
            DocumentId = "doc-1",
            ProjectId  = "proj-1",
            UserId     = "user-1",
            Title      = "Spec.pdf",
            IsFolder   = false,
        });

        var result = await _controller.Handle(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Handle_DoesNotOpenDatabase_WhenEventIsForDocument_NotFolder()
    {
        var request = BuildRequest(new DocumentDeletedEvent { DocumentId = "doc-1", IsFolder = false });

        await _controller.Handle(request, CancellationToken.None);

        await _dbContextFactory.DidNotReceive().CreateAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_DoesNotCallSoftDelete_WhenEventIsForDocument_NotFolder()
    {
        var request = BuildRequest(new DocumentDeletedEvent { DocumentId = "doc-1", IsFolder = false });

        await _controller.Handle(request, CancellationToken.None);

        await _repository.DidNotReceive().SoftDeleteDescendantsAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    // --- Folder cascade ---

    [Test]
    public async Task Handle_Returns204_WhenFolderIsDeletedWithNoDescendants()
    {
        var request = BuildRequest(new DocumentDeletedEvent
        {
            DocumentId = "folder-1",
            IsFolder   = true,
            Title      = "Empty Folder",
        });

        var result = await _controller.Handle(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Handle_CallsSoftDeleteDescendants_WithCorrectDocumentId()
    {
        var request = BuildRequest(new DocumentDeletedEvent
        {
            DocumentId = "folder-abc",
            IsFolder   = true,
        });

        await _controller.Handle(request, CancellationToken.None);

        await _repository.Received(1).SoftDeleteDescendantsAsync(_dbContext, "folder-abc");
    }

    // --- Transaction ---

    [Test]
    public async Task Handle_BeginsTransaction_WhenProcessingFolder()
    {
        var request = BuildRequest(new DocumentDeletedEvent { DocumentId = "folder-1", IsFolder = true });

        await _controller.Handle(request, CancellationToken.None);

        await _dbContext.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_CommitsTransaction_WhenProcessingFolder()
    {
        var request = BuildRequest(new DocumentDeletedEvent { DocumentId = "folder-1", IsFolder = true });

        await _controller.Handle(request, CancellationToken.None);

        await _dbContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // --- Activity logs ---

    [Test]
    public async Task Handle_WritesActivityLog_ForEachDeletedDescendant()
    {
        var descendants = new List<DeletedDescendant>
        {
            new("child-1", "Spec.pdf", false),
            new("child-2", "Sub Folder", true),
        };
        _repository.SoftDeleteDescendantsAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IReadOnlyList<DeletedDescendant>>(descendants));

        var request = BuildRequest(new DocumentDeletedEvent
        {
            DocumentId = "folder-abc",
            IsFolder   = true,
            Title      = "Parent Folder",
            ProjectId  = "proj-xyz",
            UserId     = "user-1",
        });

        await _controller.Handle(request, CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, Arg.Any<string>(), "child-1", Arg.Any<string>(), Arg.Any<string>());
        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, Arg.Any<string>(), "child-2", Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task Handle_WritesActivityLog_WithDocumentFormat()
    {
        var descendants = new List<DeletedDescendant> { new("child-1", "Spec.pdf", false) };
        _repository.SoftDeleteDescendantsAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IReadOnlyList<DeletedDescendant>>(descendants));

        var request = BuildRequest(new DocumentDeletedEvent
        {
            DocumentId = "folder-abc",
            IsFolder   = true,
            Title      = "Parent Folder",
            ProjectId  = "proj-xyz",
            UserId     = "user-1",
        });

        await _controller.Handle(request, CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "child-1", "user-1",
            "Document 'Spec.pdf' deleted (child of deleted folder 'Parent Folder')");
    }

    [Test]
    public async Task Handle_WritesActivityLog_WithFolderFormat()
    {
        var descendants = new List<DeletedDescendant> { new("child-2", "Sub Folder", true) };
        _repository.SoftDeleteDescendantsAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IReadOnlyList<DeletedDescendant>>(descendants));

        var request = BuildRequest(new DocumentDeletedEvent
        {
            DocumentId = "folder-abc",
            IsFolder   = true,
            Title      = "Parent Folder",
            ProjectId  = "proj-xyz",
            UserId     = "user-1",
        });

        await _controller.Handle(request, CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "child-2", "user-1",
            "Folder 'Sub Folder' deleted (child of deleted folder 'Parent Folder')");
    }

    [Test]
    public async Task Handle_DoesNotWriteActivityLog_WhenNoDescendants()
    {
        _repository.SoftDeleteDescendantsAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult<IReadOnlyList<DeletedDescendant>>([]));

        var request = BuildRequest(new DocumentDeletedEvent { DocumentId = "folder-1", IsFolder = true });

        await _controller.Handle(request, CancellationToken.None);

        await _repository.DidNotReceive().InsertProjectActivityLogAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>());
    }
}
