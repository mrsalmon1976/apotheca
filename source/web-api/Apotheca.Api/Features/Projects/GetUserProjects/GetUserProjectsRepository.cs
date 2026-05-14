using Apotheca.Data;
using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Projects.GetUserProjects;

public class GetUserProjectsRepository
{
    public virtual async Task<IEnumerable<ProjectDbEntity>> GetProjectsByUidAsync(IDbContext db, string firebaseUid)
    {
        return await db.QueryAsync<ProjectDbEntity>(
            @"SELECT p.id            AS Id,
                     p.name          AS Name,
                     p.summary       AS Summary,
                     p.created_at    AS CreatedAt,
                     up.project_role AS ProjectRole
              FROM projects p
              INNER JOIN user_projects up ON up.project_id = p.id
              INNER JOIN user_firebase_identities ufi ON ufi.user_id = up.user_id
              WHERE ufi.firebase_uid = @FirebaseUid
              ORDER BY p.name",
            new { FirebaseUid = firebaseUid });
    }

    public virtual async Task<IEnumerable<ProjectStatsModel>> GetProjectStatsAsync(IDbContext db, string firebaseUid)
    {
        return await db.QueryAsync<ProjectStatsModel>(
            @"SELECT p.id AS ProjectId,
                     (SELECT COUNT(1)
                      FROM user_projects up2
                      WHERE up2.project_id = p.id)                AS MemberCount,
                     (SELECT COUNT(1)
                      FROM tasks t
                      WHERE t.project_id = p.id
                        AND t.completed_at IS NULL)               AS OpenTaskCount
              FROM projects p
              INNER JOIN user_projects up ON up.project_id = p.id
              INNER JOIN user_firebase_identities ufi ON ufi.user_id = up.user_id
              WHERE ufi.firebase_uid = @FirebaseUid",
            new { FirebaseUid = firebaseUid });
    }
}
