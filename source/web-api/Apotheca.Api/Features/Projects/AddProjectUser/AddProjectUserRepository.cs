using Apotheca.Data;

namespace Apotheca.Api.Features.Projects.AddProjectUser;

public class AddProjectUserRepository
{
    public virtual async Task<string?> GetWorkspaceIdForProjectAsync(IDbContext db, string projectId)
    {
        return await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT workspace_id FROM projects WHERE id = @ProjectId",
            new { ProjectId = projectId });
    }

    public virtual async Task<bool> IsWorkspaceMemberAsync(IDbContext db, string workspaceId, string userId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM workspace_users WHERE workspace_id = @WorkspaceId AND user_id = @UserId",
            new { WorkspaceId = workspaceId, UserId = userId });
        return count > 0;
    }

    public virtual async Task<bool> IsProjectMemberAsync(IDbContext db, string projectId, string userId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM project_users WHERE project_id = @ProjectId AND user_id = @UserId",
            new { ProjectId = projectId, UserId = userId });
        return count > 0;
    }

    public virtual async Task AddMemberAsync(IDbContext db, string projectId, string userId, string projectRole)
    {
        await db.ExecuteAsync(
            "INSERT INTO project_users (user_id, project_id, project_role) VALUES (@UserId, @ProjectId, @ProjectRole)",
            new { UserId = userId, ProjectId = projectId, ProjectRole = projectRole });
    }
}
