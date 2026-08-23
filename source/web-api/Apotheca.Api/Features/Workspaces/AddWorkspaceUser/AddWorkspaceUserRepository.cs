using Apotheca.Data;

namespace Apotheca.Api.Features.Workspaces.AddWorkspaceUser;

public class AddWorkspaceUserRepository
{
    public virtual async Task<string?> GetUserIdByEmailAsync(IDbContext db, string email)
    {
        return await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT id FROM users WHERE email = @Email",
            new { Email = email });
    }

    public virtual async Task<bool> IsMemberAsync(IDbContext db, string workspaceId, string userId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM workspace_users WHERE workspace_id = @WorkspaceId AND user_id = @UserId",
            new { WorkspaceId = workspaceId, UserId = userId });
        return count > 0;
    }

    public virtual async Task AddMemberAsync(IDbContext db, string workspaceId, string userId, string workspaceRole)
    {
        await db.ExecuteAsync(
            "INSERT INTO workspace_users (workspace_id, user_id, workspace_role) VALUES (@WorkspaceId, @UserId, @WorkspaceRole)",
            new { WorkspaceId = workspaceId, UserId = userId, WorkspaceRole = workspaceRole });
    }
}
