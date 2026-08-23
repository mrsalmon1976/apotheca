using Apotheca.Data;

namespace Apotheca.Api.Features.Projects.SaveProjectUserRole;

public class SaveProjectUserRoleRepository
{
    public virtual async Task<string?> GetMemberRoleAsync(IDbContext db, string projectId, string userId)
    {
        return await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT project_role FROM project_users WHERE project_id = @ProjectId AND user_id = @UserId",
            new { ProjectId = projectId, UserId = userId });
    }

    public virtual async Task<int> CountAdminsAsync(IDbContext db, string projectId)
    {
        return await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM project_users WHERE project_id = @ProjectId AND project_role = @AdminRole",
            new { ProjectId = projectId, AdminRole = DataConstants.ProjectRole.Admin });
    }

    public virtual async Task<bool> SaveMemberRoleAsync(IDbContext db, string projectId, string userId, string projectRole)
    {
        var rows = await db.ExecuteAsync(
            "UPDATE project_users SET project_role = @ProjectRole WHERE project_id = @ProjectId AND user_id = @UserId",
            new { ProjectId = projectId, UserId = userId, ProjectRole = projectRole });
        return rows > 0;
    }
}
