using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.Workspaces.CreateWorkspace;

public class CreateWorkspaceRepository
{
    public virtual async Task<string> CreateWorkspaceAsync(IDbContext db, string name)
    {
        string workspaceId = Nanoid.Generate(DataConstants.KeyDefinition.WorkspaceAlphabet, DataConstants.KeyDefinition.WorkspaceIdLength);
        await db.ExecuteAsync(
            "INSERT INTO workspaces (id, name) VALUES (@Id, @Name)",
            new { Id = workspaceId, Name = name });

        return workspaceId;
    }

    public virtual async Task CreateWorkspaceMemberAsync(IDbContext db, string workspaceId, string userId, string workspaceRole)
    {
        await db.ExecuteAsync(
            "INSERT INTO workspace_users (workspace_id, user_id, workspace_role) VALUES (@WorkspaceId, @UserId, @WorkspaceRole)",
            new { WorkspaceId = workspaceId, UserId = userId, WorkspaceRole = workspaceRole });
    }

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
