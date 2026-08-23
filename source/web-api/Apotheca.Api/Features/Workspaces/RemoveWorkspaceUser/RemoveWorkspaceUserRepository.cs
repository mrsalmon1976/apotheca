using Apotheca.Data;

namespace Apotheca.Api.Features.Workspaces.RemoveWorkspaceUser;

public class RemoveWorkspaceUserRepository
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

    public virtual async Task RemoveProjectAccessForWorkspaceAsync(IDbContext db, string workspaceId, string userId)
    {
        await db.ExecuteAsync(
            @"DELETE FROM project_users
              WHERE user_id = @UserId
                AND project_id IN (SELECT id FROM projects WHERE workspace_id = @WorkspaceId)",
            new { WorkspaceId = workspaceId, UserId = userId });
    }

    public virtual async Task RemoveMemberAsync(IDbContext db, string workspaceId, string userId)
    {
        await db.ExecuteAsync(
            "DELETE FROM workspace_users WHERE workspace_id = @WorkspaceId AND user_id = @UserId",
            new { WorkspaceId = workspaceId, UserId = userId });
    }

    public virtual async Task ReassignCurrentWorkspaceAsync(IDbContext db, string userId, string workspaceId)
    {
        var replacementWorkspaceId = await db.QueryFirstOrDefaultAsync<string?>(
            @"SELECT workspace_id FROM workspace_users
              WHERE user_id = @UserId AND workspace_id != @WorkspaceId
              ORDER BY created_at
              LIMIT 1",
            new { UserId = userId, WorkspaceId = workspaceId });

        await db.ExecuteAsync(
            @"UPDATE user_settings
              SET current_workspace_id = @ReplacementWorkspaceId,
                  updated_at = now()
              WHERE user_id = @UserId AND current_workspace_id = @WorkspaceId",
            new { UserId = userId, WorkspaceId = workspaceId, ReplacementWorkspaceId = replacementWorkspaceId });
    }
}
