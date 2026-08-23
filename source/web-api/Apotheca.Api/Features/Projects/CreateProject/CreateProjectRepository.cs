using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.Projects.CreateProject;

public class CreateProjectRepository
{
    public virtual async Task<bool> IsWorkspaceMemberAsync(IDbContext db, string workspaceId, string userId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM workspace_users WHERE workspace_id = @WorkspaceId AND user_id = @UserId",
            new { WorkspaceId = workspaceId, UserId = userId });
        return count > 0;
    }

    public virtual async Task<string> CreateProjectAsync(IDbContext db, string workspaceId, string name, string? summary)
    {
        string projectId = Nanoid.Generate(DataConstants.KeyDefinition.ProjectAlphabet, DataConstants.KeyDefinition.ProjectIdLength);
        await db.ExecuteAsync(
            "INSERT INTO projects (id, workspace_id, name, summary) VALUES (@Id, @WorkspaceId, @Name, @Summary)",
            new { Id = projectId, WorkspaceId = workspaceId, Name = name, Summary = summary });

        return projectId;
    }

    public virtual async Task AddProjectMemberAsync(IDbContext db, string projectId, string userId, string projectRole)
    {
        await db.ExecuteAsync(
            @"INSERT INTO project_users (user_id, project_id, project_role)
              VALUES (@UserId, @ProjectId, @ProjectRole)
              ON CONFLICT (user_id, project_id) DO NOTHING",
            new { UserId = userId, ProjectId = projectId, ProjectRole = projectRole });
    }
}
