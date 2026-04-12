using Apotheca.Data;
using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Projects.GetProjectActivity;

public class GetProjectActivityRepository
{
    public virtual async Task<bool> UserHasProjectAccessAsync(
        IDbContext db, string firebaseUid, string projectId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            @"SELECT COUNT(1)
              FROM user_projects up
              INNER JOIN user_firebase_identities ufi ON ufi.user_id = up.user_id
              WHERE ufi.firebase_uid = @FirebaseUid
                AND up.project_id = @ProjectId",
            new { FirebaseUid = firebaseUid, ProjectId = projectId });
        return count > 0;
    }

    public virtual async Task<IEnumerable<ProjectActivityLogDbEntity>> GetProjectActivityAsync(
        IDbContext db, string projectId)
    {
        return await db.QueryAsync<ProjectActivityLogDbEntity>(
            @"SELECT pal.id          AS Id,
                     pal.project_id  AS ProjectId,
                     pal.ref_id      AS RefId,
                     pal.ref_type    AS RefType,
                     pal.log_message AS LogMessage,
                     pal.user_id     AS UserId,
                     u.display_name  AS Username,
                     pal.created_at  AS CreatedAt
              FROM audit.project_activity_logs pal
              INNER JOIN users u ON u.id = pal.user_id
              WHERE pal.project_id = @ProjectId
              ORDER BY pal.created_at DESC
              LIMIT 50",
            new { ProjectId = projectId });
    }
}
