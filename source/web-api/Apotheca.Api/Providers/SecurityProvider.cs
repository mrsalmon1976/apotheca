using Apotheca.Data;

namespace Apotheca.Api.Providers;

public class SecurityProvider(IHttpContextAccessor httpContextAccessor) : ISecurityProvider
{
    public async Task<SecurityResult> AuthorizeAccessAsync(IDbContext db, CancellationToken cancellationToken = default)
    {
        var firebaseUid = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(firebaseUid))
            return SecurityResult.Failure("User identity could not be determined.");

        var userId = await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT user_id FROM user_firebase_identities WHERE firebase_uid = @FirebaseUid",
            new { FirebaseUid = firebaseUid });

        if (userId is null)
            return SecurityResult.Failure("User identity could not be determined.");

        return SecurityResult.Success(firebaseUid, userId);
    }

    public async Task<SecurityResult> AuthorizeProjectAccessAsync(IDbContext db, string projectId, CancellationToken cancellationToken = default)
    {
        var result = await this.AuthorizeAccessAsync(db, cancellationToken);
        if (!result.IsAuthorized)
        {
            return result;
        }

        var hasAccess = await db.QueryFirstOrDefaultAsync<int>(
            @"SELECT COUNT(1)
              FROM user_projects up
              INNER JOIN user_firebase_identities ufi ON ufi.user_id = up.user_id
              WHERE ufi.firebase_uid = @FirebaseUid
                AND up.project_id = @ProjectId",
            new { FirebaseUid = result.FirebaseUid, ProjectId = projectId });

        if (hasAccess == 0)
            return SecurityResult.Failure("User does not have access to this project.");

        return SecurityResult.Success(result.FirebaseUid, result.UserId);
    }

}
