using Apotheca.Api.Features.Auth.Login;
using Apotheca.Data;
using Apotheca.Test.Common;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Apotheca.Api.Tests.Features.Auth.Login;

[TestFixture]
public class LoginControllerTests
{
    private IDbContextFactory _dbContextFactory = null!;
    private IDbContext _dbContext = null!;
    private FirebaseService _firebaseService = null!;
    private LoginRepository _loginRepository = null!;
    private LoginController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory>();
        _dbContext = Substitute.For<IDbContext>();
        _firebaseService = Substitute.For<FirebaseService>();
        _loginRepository = Substitute.For<LoginRepository>();

        _dbContextFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_dbContext));

        _controller = new LoginController(_dbContextFactory, _firebaseService, _loginRepository);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    // --- Result shape ---

    [Test]
    public async Task Login_ReturnsOk_WhenIdentityExists()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));

        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid).Returns(Task.FromResult(true));

        var result = await _controller.Login(loginRequest, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task Login_ReturnsOk_WhenNewUserCreatedSuccessfully()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));

        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid)
            .Returns(Task.FromResult(false));
        _loginRepository.GetUserIdByEmailAsync(_dbContext, user.Email)
            .Returns(Task.FromResult<string?>(null));
        _loginRepository.CreateUserAsync(_dbContext, user)
            .Returns(Task.FromResult("new-user-id"));
        _loginRepository.CreateProjectAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult("new-project-id"));

        var result = await _controller.Login(loginRequest, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task Login_ReturnsOk_WhenIdentityMissingButUserExistsByEmail()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));

        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid)
            .Returns(Task.FromResult(false));
        _loginRepository.GetUserIdByEmailAsync(_dbContext, user.Email)
            .Returns(Task.FromResult<string?>("existing-user-id"));

        var result = await _controller.Login(loginRequest, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task Login_ReturnsUnauthorized_WhenFirebaseThrowsUnauthorizedAccessException()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException("Invalid or expired token."));

        var result = await _controller.Login(loginRequest, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task Login_ReturnsUnauthorized_WithErrorMessage_WhenFirebaseThrowsUnauthorizedAccessException()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException("Invalid or expired token."));

        var result = (UnauthorizedObjectResult)await _controller.Login(loginRequest, CancellationToken.None);
        var error = result.Value?.GetType().GetProperty("error")?.GetValue(result.Value)?.ToString();

        Assert.That(error, Is.EqualTo("Invalid or expired token."));
    }

    [Test]
    public async Task Login_Returns500_WhenFirebaseThrowsUnexpectedException()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Unexpected error."));

        var result = (ObjectResult)await _controller.Login(loginRequest, CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(500));
    }

    // --- User/identity creation ---

    [Test]
    public async Task Login_CreatesUser_WhenIdentityAndUserDoNotExist()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));

        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid)
            .Returns(Task.FromResult(false));
        _loginRepository.GetUserIdByEmailAsync(_dbContext, user.Email)
            .Returns(Task.FromResult<string?>(null));
        _loginRepository.CreateUserAsync(_dbContext, user)
            .Returns(Task.FromResult("new-user-id"));
        _loginRepository.CreateProjectAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult("new-project-id"));

        await _controller.Login(loginRequest, CancellationToken.None);

        await _loginRepository.Received(1).CreateUserAsync(_dbContext, user);
    }

    [Test]
    public async Task Login_DoesNotCreateUser_WhenUserAlreadyExistsByEmail()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));

        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid)
            .Returns(Task.FromResult(false));
        _loginRepository.GetUserIdByEmailAsync(_dbContext, user.Email)
            .Returns(Task.FromResult<string?>("existing-user-id"));

        await _controller.Login(loginRequest, CancellationToken.None);

        await _loginRepository.DidNotReceive().CreateUserAsync(Arg.Any<IDbContext>(), Arg.Any<User>());
    }

    [Test]
    public async Task Login_CreatesIdentity_WhenIdentityDoesNotExist()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));

        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid)
            .Returns(Task.FromResult(false));
        _loginRepository.GetUserIdByEmailAsync(_dbContext, user.Email)
            .Returns(Task.FromResult<string?>("existing-user-id"));

        await _controller.Login(loginRequest, CancellationToken.None);

        await _loginRepository.Received(1).CreateUserIdentityAsync(_dbContext, user, "existing-user-id");
    }

    [Test]
    public async Task Login_DoesNotCreateIdentity_WhenIdentityAlreadyExists()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));

        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid)
            .Returns(Task.FromResult(true));

        await _controller.Login(loginRequest, CancellationToken.None);

        await _loginRepository.DidNotReceive().CreateUserIdentityAsync(Arg.Any<IDbContext>(), Arg.Any<User>(), Arg.Any<string>());
    }

    // --- Transaction management ---

    [Test]
    public async Task Login_CommitsTransaction_WhenIdentityDoesNotExist()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));

        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid).Returns(Task.FromResult(false));
        _loginRepository.GetUserIdByEmailAsync(_dbContext, user.Email).Returns(Task.FromResult<string?>("existing-user-id"));

        await _controller.Login(loginRequest, CancellationToken.None);

        await _dbContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Login_DoesNotBeginTransaction_WhenIdentityExists()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));
        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid)
            .Returns(Task.FromResult(true));

        await _controller.Login(loginRequest, CancellationToken.None);

        await _dbContext.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Login_RollsBackTransaction_WhenExceptionOccursDuringUserCreation()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));
        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid)
            .Returns(Task.FromResult(false));
        _loginRepository.GetUserIdByEmailAsync(_dbContext, user.Email)
            .Returns(Task.FromResult<string?>(null));
        _loginRepository.CreateUserAsync(_dbContext, Arg.Any<User>())
            .Throws(new Exception("DB error"));

        Assert.ThrowsAsync<Exception>(() => _controller.Login(loginRequest, CancellationToken.None));

        await _dbContext.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    // --- Project creation ---

    [Test]
    public async Task Login_CreatesProject_WhenNewUserCreated()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));

        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid)
            .Returns(Task.FromResult(false));
        _loginRepository.GetUserIdByEmailAsync(_dbContext, user.Email)
            .Returns(Task.FromResult<string?>(null));
        _loginRepository.CreateUserAsync(_dbContext, user)
            .Returns(Task.FromResult("new-user-id"));
        _loginRepository.CreateProjectAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult("new-project-id"));

        await _controller.Login(loginRequest, CancellationToken.None);

        await _loginRepository.Received(1).CreateProjectAsync(_dbContext, Arg.Any<string>());
    }

    [Test]
    public async Task Login_DoesNotCreateProject_WhenUserAlreadyExistsByEmail()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));

        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid)
            .Returns(Task.FromResult(false));
        _loginRepository.GetUserIdByEmailAsync(_dbContext, user.Email)
            .Returns(Task.FromResult<string?>("existing-user-id"));

        await _controller.Login(loginRequest, CancellationToken.None);

        await _loginRepository.DidNotReceive().CreateProjectAsync(Arg.Any<IDbContext>(), Arg.Any<string>());
    }

    [Test]
    public async Task Login_CreatesUserProject_WhenNewUserCreated()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));

        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid)
            .Returns(Task.FromResult(false));
        _loginRepository.GetUserIdByEmailAsync(_dbContext, user.Email)
            .Returns(Task.FromResult<string?>(null));
        _loginRepository.CreateUserAsync(_dbContext, user)
            .Returns(Task.FromResult("new-user-id"));
        _loginRepository.CreateProjectAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult("new-project-id"));

        await _controller.Login(loginRequest, CancellationToken.None);

        await _loginRepository.Received(1).CreateUserProjectAsync(_dbContext, "new-user-id", "new-project-id", DataConstants.ProjectRole.Owner);
    }

    [Test]
    public async Task Login_DoesNotCreateUserProject_WhenUserAlreadyExistsByEmail()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));

        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid)
            .Returns(Task.FromResult(false));
        _loginRepository.GetUserIdByEmailAsync(_dbContext, user.Email)
            .Returns(Task.FromResult<string?>("existing-user-id"));

        await _controller.Login(loginRequest, CancellationToken.None);

        await _loginRepository.DidNotReceive().CreateUserProjectAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task Login_CreatesProjectAuditLog_WhenNewUserCreated()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));

        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid)
            .Returns(Task.FromResult(false));
        _loginRepository.GetUserIdByEmailAsync(_dbContext, user.Email)
            .Returns(Task.FromResult<string?>(null));
        _loginRepository.CreateUserAsync(_dbContext, user)
            .Returns(Task.FromResult("new-user-id"));
        _loginRepository.CreateProjectAsync(_dbContext, Arg.Any<string>())
            .Returns(Task.FromResult("new-project-id"));

        await _controller.Login(loginRequest, CancellationToken.None);

        await _loginRepository.Received(1).CreateProjectAuditLogAsync(_dbContext, "new-project-id", "new-user-id");
    }

    [Test]
    public async Task Login_DoesNotCreateProjectAuditLog_WhenUserAlreadyExistsByEmail()
    {
        var loginRequest = RandomData.Create<LoginRequest>();

        var user = RandomData.Create<User>();
        _firebaseService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(user));

        _loginRepository.UserFirebaseIdentityExistsAsync(_dbContext, user.Uid)
            .Returns(Task.FromResult(false));
        _loginRepository.GetUserIdByEmailAsync(_dbContext, user.Email)
            .Returns(Task.FromResult<string?>("existing-user-id"));

        await _controller.Login(loginRequest, CancellationToken.None);

        await _loginRepository.DidNotReceive().CreateProjectAuditLogAsync(Arg.Any<IDbContext>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
