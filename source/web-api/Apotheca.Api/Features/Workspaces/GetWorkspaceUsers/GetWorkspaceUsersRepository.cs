using Apotheca.Data;
using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Workspaces.GetWorkspaceUsers;

public class GetWorkspaceUsersRepository
{
    public virtual async Task<IEnumerable<WorkspaceUserDbEntity>> GetMembersAsync(IDbContext db, string workspaceId)
    {
        return await db.QueryAsync<WorkspaceUserDbEntity>(
            @"SELECT u.id             AS UserId,
                     u.email          AS Email,
                     u.display_name   AS DisplayName,
                     u.photo_url      AS PhotoUrl,
                     wm.workspace_role AS WorkspaceRole,
                     wm.created_at    AS CreatedAt
              FROM workspace_users wm
              INNER JOIN users u ON u.id = wm.user_id
              WHERE wm.workspace_id = @WorkspaceId
              ORDER BY u.display_name",
            new { WorkspaceId = workspaceId });
    }
}
