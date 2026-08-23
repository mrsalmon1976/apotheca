using Apotheca.Data;

namespace Apotheca.Api.Features.Workspaces.SwitchCurrentWorkspace;

public class SwitchCurrentWorkspaceRepository
{
    public virtual async Task SetCurrentWorkspaceAsync(IDbContext db, string userId, string workspaceId)
    {
        await db.ExecuteAsync(
            @"INSERT INTO user_settings (user_id, current_workspace_id)
              VALUES (@UserId, @WorkspaceId)
              ON CONFLICT (user_id) DO UPDATE
              SET current_workspace_id = EXCLUDED.current_workspace_id,
                  updated_at = now()",
            new { UserId = userId, WorkspaceId = workspaceId });
    }
}
