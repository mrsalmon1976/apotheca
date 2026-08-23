using Apotheca.Data;

namespace Apotheca.Api.Features.Workspaces.SaveWorkspaceUserRole;

public class SaveWorkspaceUserRoleRepository
{
    public virtual async Task<string?> GetMemberRoleAsync(IDbContext db, string workspaceId, string userId)
    {
        return await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT workspace_role FROM workspace_users WHERE workspace_id = @WorkspaceId AND user_id = @UserId",
            new { WorkspaceId = workspaceId, UserId = userId });
    }

    public virtual async Task<int> CountAdminsAsync(IDbContext db, string workspaceId)
    {
        return await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM workspace_users WHERE workspace_id = @WorkspaceId AND workspace_role = @AdminRole",
            new { WorkspaceId = workspaceId, AdminRole = DataConstants.WorkspaceRole.Admin });
    }

    public virtual async Task<bool> SaveMemberRoleAsync(IDbContext db, string workspaceId, string userId, string workspaceRole)
    {
        var rows = await db.ExecuteAsync(
            "UPDATE workspace_users SET workspace_role = @WorkspaceRole WHERE workspace_id = @WorkspaceId AND user_id = @UserId",
            new { WorkspaceId = workspaceId, UserId = userId, WorkspaceRole = workspaceRole });
        return rows > 0;
    }
}
