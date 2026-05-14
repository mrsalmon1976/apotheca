using System.Text;
using System.Text.Json;
using Apotheca.Api.Events;
using Apotheca.Api.Events.Documents.DocumentRestored;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Apotheca.Api.Tests.Events.Documents.DocumentRestored;

[TestFixture]
public class DocumentRestoredEventHandlerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private DocumentRestoredRepository _repository = null!;
    private DocumentRestoredEventHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<DocumentRestoredRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _handler = new DocumentRestoredEventHandler(
            _dbContextFactory, _repository, Substitute.For<ILogger<DocumentRestoredEventHandler>>());
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private static PubSubPushRequest BuildRequest(DocumentRestoredEvent eventData)
    {
        var json    = JsonSerializer.Serialize(eventData);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return new PubSubPushRequest
        {
            Message      = new PubSubMessage { Data = encoded },
            Subscription = "projects/test/subscriptions/document-restored-sub",
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

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestResult>());
    }

    // --- No ancestors short circuit ---

    [Test]
    public async Task Handle_Returns204_WhenThereAreNoRestoredAncestors()
    {
        var request = BuildRequest(new DocumentRestoredEvent
        {
            DocumentId        = "doc-1",
            ProjectId         = "proj-1",
            UserId            = "user-1",
            Title             = "Spec.pdf",
            RestoredAncestors = [],
        });

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Handle_DoesNotOpenDatabase_WhenThereAreNoRestoredAncestors()
    {
        var request = BuildRequest(new DocumentRestoredEvent
        {
            DocumentId        = "doc-1",
            RestoredAncestors = [],
        });

        await _handler.Handle(request, CancellationToken.None);

        await _dbContextFactory.DidNotReceive().CreateAsync(Arg.Any<CancellationToken>());
    }

    // --- Transaction ---

    [Test]
    public async Task Handle_BeginsTransaction_WhenAncestorsExist()
    {
        var request = BuildRequest(new DocumentRestoredEvent
        {
            DocumentId        = "doc-1",
            RestoredAncestors = [new RestoredAncestor { DocumentId = "ancestor-1", Title = "Parent", IsFolder = true }],
        });

        await _handler.Handle(request, CancellationToken.None);

        await _dbContext.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_CommitsTransaction_WhenAncestorsExist()
    {
        var request = BuildRequest(new DocumentRestoredEvent
        {
            DocumentId        = "doc-1",
            RestoredAncestors = [new RestoredAncestor { DocumentId = "ancestor-1", Title = "Parent", IsFolder = true }],
        });

        await _handler.Handle(request, CancellationToken.None);

        await _dbContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // --- Activity logs ---

    [Test]
    public async Task Handle_WritesActivityLog_ForEachRestoredAncestor()
    {
        var request = BuildRequest(new DocumentRestoredEvent
        {
            DocumentId = "doc-1",
            ProjectId  = "proj-xyz",
            UserId     = "user-1",
            Title      = "Spec.pdf",
            IsFolder   = false,
            RestoredAncestors =
            [
                new RestoredAncestor { DocumentId = "ancestor-1", Title = "Parent Folder", IsFolder = true },
                new RestoredAncestor { DocumentId = "ancestor-2", Title = "Grandparent Folder", IsFolder = true },
            ],
        });

        await _handler.Handle(request, CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, Arg.Any<string>(), "ancestor-1", Arg.Any<string>(), Arg.Any<string>());
        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, Arg.Any<string>(), "ancestor-2", Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task Handle_WritesActivityLog_WithFolderAncestorFormat_WhenRestoredItemIsDocument()
    {
        var request = BuildRequest(new DocumentRestoredEvent
        {
            DocumentId = "doc-abc",
            ProjectId  = "proj-xyz",
            UserId     = "user-1",
            Title      = "Spec.pdf",
            IsFolder   = false,
            RestoredAncestors =
            [
                new RestoredAncestor { DocumentId = "ancestor-1", Title = "Parent Folder", IsFolder = true },
            ],
        });

        await _handler.Handle(request, CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "ancestor-1", "user-1",
            "Folder 'Parent Folder' restored (parent of restored document 'Spec.pdf')");
    }

    [Test]
    public async Task Handle_WritesActivityLog_WithFolderAncestorFormat_WhenRestoredItemIsFolder()
    {
        var request = BuildRequest(new DocumentRestoredEvent
        {
            DocumentId = "folder-abc",
            ProjectId  = "proj-xyz",
            UserId     = "user-1",
            Title      = "Archive",
            IsFolder   = true,
            RestoredAncestors =
            [
                new RestoredAncestor { DocumentId = "ancestor-1", Title = "Parent Folder", IsFolder = true },
            ],
        });

        await _handler.Handle(request, CancellationToken.None);

        await _repository.Received(1).InsertProjectActivityLogAsync(
            _dbContext, "proj-xyz", "ancestor-1", "user-1",
            "Folder 'Parent Folder' restored (parent of restored folder 'Archive')");
    }

    [Test]
    public async Task Handle_Returns204_AfterWritingLogs()
    {
        var request = BuildRequest(new DocumentRestoredEvent
        {
            DocumentId        = "doc-1",
            RestoredAncestors = [new RestoredAncestor { DocumentId = "ancestor-1", Title = "Parent", IsFolder = true }],
        });

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }
}
