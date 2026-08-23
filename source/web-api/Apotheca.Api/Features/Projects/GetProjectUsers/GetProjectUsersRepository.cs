using Apotheca.Data;
using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Projects.GetProjectUsers;

public class GetProjectUsersRepository
{
    public virtual async Task<IEnumerable<ProjectUserDbEntity>> GetMembersAsync(IDbContext db, string projectId)
    {
        return await db.QueryAsync<ProjectUserDbEntity>(
            @"SELECT u.id           AS UserId,
                     u.email        AS Email,
                     u.display_name AS DisplayName,
                     u.photo_url    AS PhotoUrl,
                     up.project_role AS ProjectRole,
                     up.created_at  AS CreatedAt
              FROM project_users up
              INNER JOIN users u ON u.id = up.user_id
              WHERE up.project_id = @ProjectId
              ORDER BY u.display_name",
            new { ProjectId = projectId });
    }
}
