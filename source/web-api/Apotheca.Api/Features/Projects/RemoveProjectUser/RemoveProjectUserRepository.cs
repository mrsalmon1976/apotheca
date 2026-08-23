using Apotheca.Data;

namespace Apotheca.Api.Features.Projects.RemoveProjectUser;

public class RemoveProjectUserRepository
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

    public virtual async Task RemoveMemberAsync(IDbContext db, string projectId, string userId)
    {
        await db.ExecuteAsync(
            "DELETE FROM project_users WHERE project_id = @ProjectId AND user_id = @UserId",
            new { ProjectId = projectId, UserId = userId });
    }
}
