using Apotheca.Data;
using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Users.GetUserWorkspaces;

public class GetUserWorkspacesRepository
{
    public virtual async Task<IEnumerable<WorkspaceDbEntity>> GetWorkspacesByUidAsync(IDbContext db, string firebaseUid)
    {
        return await db.QueryAsync<WorkspaceDbEntity>(
            @"SELECT w.id             AS Id,
                     w.name           AS Name,
                     wm.workspace_role AS WorkspaceRole,
                     w.plan           AS Plan,
                     w.billing_status AS BillingStatus,
                     w.created_at     AS CreatedAt,
                     COALESCE(us.current_workspace_id = w.id, false) AS IsCurrent
              FROM workspaces w
              INNER JOIN workspace_users wm ON wm.workspace_id = w.id
              INNER JOIN user_firebase_identities ufi ON ufi.user_id = wm.user_id
              LEFT JOIN user_settings us ON us.user_id = wm.user_id
              WHERE ufi.firebase_uid = @FirebaseUid
              ORDER BY w.name",
            new { FirebaseUid = firebaseUid });
    }

    public virtual async Task<IEnumerable<WorkspaceStatsModel>> GetWorkspaceStatsAsync(IDbContext db, string firebaseUid)
    {
        return await db.QueryAsync<WorkspaceStatsModel>(
            @"SELECT w.id AS WorkspaceId,
                     (SELECT COUNT(1) FROM workspace_users wm2 WHERE wm2.workspace_id = w.id) AS MemberCount,
                     (SELECT COUNT(1) FROM projects p WHERE p.workspace_id = w.id)               AS ProjectCount
              FROM workspaces w
              INNER JOIN workspace_users wm ON wm.workspace_id = w.id
              INNER JOIN user_firebase_identities ufi ON ufi.user_id = wm.user_id
              WHERE ufi.firebase_uid = @FirebaseUid",
            new { FirebaseUid = firebaseUid });
    }
}
