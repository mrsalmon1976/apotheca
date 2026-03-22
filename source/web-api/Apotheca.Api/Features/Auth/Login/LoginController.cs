using Apotheca.Data;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Auth.Login;

[ApiController]
[Route("api/auth")]
public class LoginController(IDbContextFactory dbContextFactory
    , FirebaseService firebaseService
    , LoginRepository loginRepo) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        User user;

        try
        {
            user = await firebaseService.LoginAsync(request, cancellationToken);
        }
        catch (UnauthorizedAccessException uaex)
        {
            return Unauthorized(new { error = uaex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var identityExists = await loginRepo.UserFirebaseIdentityExistsAsync(db, user.Uid);

        if (!identityExists)
        {
            await db.BeginTransactionAsync(cancellationToken);

            try
            {
                var userId = await loginRepo.GetUserIdByEmailAsync(db, user.Email);

                if (userId is null)
                {
                    userId = await loginRepo.CreateUserAsync(db, user);

                    var projectId = await loginRepo.CreateProjectAsync(db, DataConstants.DefaultProjectName);
                    await loginRepo.CreateUserProjectAsync(db, userId, projectId, DataConstants.ProjectRole.Owner);
                    await loginRepo.CreateProjectAuditLogAsync(db, projectId, userId);
                }

                await loginRepo.CreateUserIdentityAsync(db, user, userId);

                await db.CommitAsync(cancellationToken);
            }
            catch
            {
                await db.RollbackAsync(cancellationToken);
                throw;
            }

        }

        return Ok();
    }
}
