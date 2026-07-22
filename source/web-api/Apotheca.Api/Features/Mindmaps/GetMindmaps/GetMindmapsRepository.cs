using Apotheca.Data;

namespace Apotheca.Api.Features.Mindmaps.GetMindmaps;

public class GetMindmapsRepository
{
    public virtual async Task<IEnumerable<GetMindmapsResponse>> GetMindmapsAsync(IDbContext db, string projectId)
    {
        return await db.QueryAsync<GetMindmapsResponse>(
            @"SELECT id AS Id, name AS Name, updated_at AS UpdatedAt
              FROM mindmaps
              WHERE project_id = @ProjectId
                AND deleted_at IS NULL
              ORDER BY updated_at DESC",
            new { ProjectId = projectId });
    }
}
