using Apotheca.Api.Features.Search;
using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Apotheca.Api.Tests.Features.Search;

[TestFixture]
public class SearchControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext        _dbContext        = null!;
    private SearchRepository  _repository       = null!;
    private ISecurityProvider _securityProvider = null!;
    private SearchController  _controller       = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext        = Substitute.For<IDbContext>();
        _repository       = Substitute.For<SearchRepository>();
        _securityProvider = Substitute.For<ISecurityProvider>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _repository
            .SearchAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(Task.FromResult(Enumerable.Empty<SearchResult>()));

        _controller = new SearchController(_dbContextFactory, _repository, _securityProvider);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    private void AllowAccess(string userId = "user-id-123")
    {
        _securityProvider
            .AuthorizeAccessAsync(_dbContext, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Success("firebase-uid", userId)));
    }

    private void DenyAccess(string errorMessage = "User identity could not be determined.")
    {
        _securityProvider
            .AuthorizeAccessAsync(_dbContext, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SecurityResult.Failure(errorMessage)));
    }

    // --- Short-circuit: blank / too-short query ---

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("a")]
    public async Task Search_ReturnsOk_WhenQueryIsTooShort(string? q)
    {
        var result = await _controller.Search(q, cancellationToken: CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("a")]
    public async Task Search_ReturnsEmptyList_WhenQueryIsTooShort(string? q)
    {
        var result = (OkObjectResult)await _controller.Search(q, cancellationToken: CancellationToken.None);
        var items  = result.Value as IEnumerable<SearchResult>;

        Assert.That(items, Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("a")]
    public async Task Search_DoesNotOpenDatabase_WhenQueryIsTooShort(string? q)
    {
        await _controller.Search(q, cancellationToken: CancellationToken.None);

        await _dbContextFactory.DidNotReceive().CreateAsync(Arg.Any<CancellationToken>());
    }

    // --- Identity / Access control ---

    [Test]
    public async Task Search_Returns401_WhenIdentityFails()
    {
        DenyAccess("User identity could not be determined.");

        var result = await _controller.Search("invoices", cancellationToken: CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task Search_Returns401_WithErrorMessage_WhenIdentityFails()
    {
        DenyAccess("User identity could not be determined.");

        var result = (UnauthorizedObjectResult)await _controller.Search("invoices", cancellationToken: CancellationToken.None);
        var error  = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("User identity could not be determined."));
    }

    [Test]
    public async Task Search_DoesNotSearch_WhenAccessDenied()
    {
        DenyAccess();

        await _controller.Search("invoices", cancellationToken: CancellationToken.None);

        await _repository.DidNotReceive().SearchAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string[]>(), Arg.Any<bool>(), Arg.Any<bool>());
    }

    // --- Result shape ---

    [Test]
    public async Task Search_ReturnsOk_WhenAuthorized()
    {
        AllowAccess();

        var result = await _controller.Search("invoices", cancellationToken: CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Search_ReturnsEmptyList_WhenRepositoryReturnsNothing()
    {
        AllowAccess();

        var result = (OkObjectResult)await _controller.Search("invoices", cancellationToken: CancellationToken.None);
        var items  = result.Value as IEnumerable<SearchResult>;

        Assert.That(items, Is.Empty);
    }

    [Test]
    public async Task Search_ReturnsResultsFromRepository()
    {
        AllowAccess();

        var repoResults = new[]
        {
            new SearchResult { ReferenceId = "note-1", ReferenceType = "note", Title = "Invoice Notes", Snippet = "Matched <b>invoice</b>", ProjectId = "proj-1" },
            new SearchResult { ReferenceId = "task-1", ReferenceType = "task", Title = "Review invoices", Snippet = "Task <b>invoice</b> due", ProjectId = "proj-1" },
        };
        _repository
            .SearchAsync(_dbContext, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(Task.FromResult<IEnumerable<SearchResult>>(repoResults));

        var result = (OkObjectResult)await _controller.Search("invoice", cancellationToken: CancellationToken.None);
        var items  = (result.Value as IEnumerable<SearchResult>)!.ToList();

        Assert.That(items, Has.Count.EqualTo(2));
        Assert.That(items[0].ReferenceId,   Is.EqualTo("note-1"));
        Assert.That(items[0].ReferenceType, Is.EqualTo("note"));
        Assert.That(items[0].Title,         Is.EqualTo("Invoice Notes"));
        Assert.That(items[1].ReferenceId,   Is.EqualTo("task-1"));
    }

    // --- Query forwarding ---

    [Test]
    public async Task Search_PassesTrimmedQuery_ToRepository()
    {
        AllowAccess();

        await _controller.Search("  invoices  ", cancellationToken: CancellationToken.None);

        await _repository.Received(1).SearchAsync(
            _dbContext, Arg.Any<string>(), "invoices",
            Arg.Any<string[]>(), Arg.Any<bool>(), Arg.Any<bool>());
    }

    [Test]
    public async Task Search_PassesUserId_ToRepository()
    {
        AllowAccess("user-abc");

        await _controller.Search("invoices", cancellationToken: CancellationToken.None);

        await _repository.Received(1).SearchAsync(
            _dbContext, "user-abc", Arg.Any<string>(),
            Arg.Any<string[]>(), Arg.Any<bool>(), Arg.Any<bool>());
    }

    // --- Type parsing ---

    [Test]
    public async Task Search_PassesDefaultTypes_WhenTypesNotSpecified()
    {
        AllowAccess();

        await _controller.Search("invoices", cancellationToken: CancellationToken.None);

        await _repository.Received(1).SearchAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<string[]>(t => t.Contains("note") && t.Contains("task")),
            Arg.Any<bool>(), Arg.Any<bool>());
    }

    [Test]
    public async Task Search_ParsesSingleType()
    {
        AllowAccess();

        await _controller.Search("invoices", types: "note", cancellationToken: CancellationToken.None);

        await _repository.Received(1).SearchAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<string[]>(t => t.SequenceEqual(new[] { "note" })),
            Arg.Any<bool>(), Arg.Any<bool>());
    }

    [Test]
    public async Task Search_NormalizesTypesToLowercase()
    {
        AllowAccess();

        await _controller.Search("invoices", types: "NOTE,TASK", cancellationToken: CancellationToken.None);

        await _repository.Received(1).SearchAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<string[]>(t => t.Contains("note") && t.Contains("task") && t.All(v => v == v.ToLowerInvariant())),
            Arg.Any<bool>(), Arg.Any<bool>());
    }

    [Test]
    public async Task Search_DeduplicatesTypes()
    {
        AllowAccess();

        await _controller.Search("invoices", types: "note,note,task", cancellationToken: CancellationToken.None);

        await _repository.Received(1).SearchAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<string[]>(t => t.Length == 2),
            Arg.Any<bool>(), Arg.Any<bool>());
    }

    [Test]
    public async Task Search_FallsBackToDefaultTypes_WhenTypesResolvesToEmpty()
    {
        AllowAccess();

        await _controller.Search("invoices", types: " , ", cancellationToken: CancellationToken.None);

        await _repository.Received(1).SearchAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<string[]>(t => t.Contains("note") && t.Contains("task")),
            Arg.Any<bool>(), Arg.Any<bool>());
    }

    // --- Field parsing ---

    [Test]
    public async Task Search_SetsBothSearchFlags_WhenFieldsIsDefault()
    {
        AllowAccess();

        await _controller.Search("invoices", cancellationToken: CancellationToken.None);

        await _repository.Received(1).SearchAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string[]>(), searchTitle: true, searchBody: true);
    }

    [Test]
    public async Task Search_SetsTitleOnly_WhenFieldsIsTitle()
    {
        AllowAccess();

        await _controller.Search("invoices", fields: "title", cancellationToken: CancellationToken.None);

        await _repository.Received(1).SearchAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string[]>(), searchTitle: true, searchBody: false);
    }

    [Test]
    public async Task Search_SetsBodyOnly_WhenFieldsIsBody()
    {
        AllowAccess();

        await _controller.Search("invoices", fields: "body", cancellationToken: CancellationToken.None);

        await _repository.Received(1).SearchAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string[]>(), searchTitle: false, searchBody: true);
    }

    [Test]
    public async Task Search_SetsBodyTrue_WhenFieldsIsEmpty()
    {
        AllowAccess();

        await _controller.Search("invoices", fields: " , ", cancellationToken: CancellationToken.None);

        await _repository.Received(1).SearchAsync(
            Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string[]>(), Arg.Any<bool>(), searchBody: true);
    }
}
