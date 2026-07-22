using Apotheca.Data;

namespace Apotheca.Api.Features.Mindmaps.SaveMindmap;

public class SaveMindmapRepository
{
    public virtual async Task<bool> MindmapExistsAsync(IDbContext db, string projectId, string mindmapId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM mindmaps WHERE id = @MindmapId AND project_id = @ProjectId AND deleted_at IS NULL",
            new { MindmapId = mindmapId, ProjectId = projectId });
        return count > 0;
    }

    public virtual async Task UpdateMindmapNameAsync(IDbContext db, string projectId, string mindmapId, string name)
    {
        await db.ExecuteAsync(
            @"UPDATE mindmaps
              SET name = @Name, updated_at = now()
              WHERE id = @MindmapId AND project_id = @ProjectId",
            new { Name = name, MindmapId = mindmapId, ProjectId = projectId });
    }

    public virtual async Task UpdateSearchTitleAsync(IDbContext db, string mindmapId, string name)
    {
        await db.ExecuteAsync(
            @"UPDATE search
              SET text_title = @Name, updated_at = now()
              WHERE reference_id = @MindmapId AND reference_type = 'mindmap'",
            new { Name = name, MindmapId = mindmapId });
    }
}
