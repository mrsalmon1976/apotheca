using Apotheca.Data;

namespace Apotheca.Api.Features.Mindmaps.DeleteMindmap;

public class DeleteMindmapRepository
{
    public virtual async Task<bool> MindmapExistsAsync(IDbContext db, string projectId, string mindmapId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM mindmaps WHERE id = @MindmapId AND project_id = @ProjectId AND deleted_at IS NULL",
            new { MindmapId = mindmapId, ProjectId = projectId });
        return count > 0;
    }

    public virtual async Task SoftDeleteMindmapAsync(IDbContext db, string mindmapId)
    {
        await db.ExecuteAsync(
            "UPDATE mindmaps SET deleted_at = now() AT TIME ZONE 'UTC' WHERE id = @MindmapId AND deleted_at IS NULL",
            new { MindmapId = mindmapId });
    }

    public virtual async Task SoftDeleteMindmapNodesAsync(IDbContext db, string mindmapId)
    {
        await db.ExecuteAsync(
            "UPDATE mindmap_nodes SET deleted_at = now() AT TIME ZONE 'UTC' WHERE mindmap_id = @MindmapId AND deleted_at IS NULL",
            new { MindmapId = mindmapId });
    }
}
