using Apotheca.Data;

namespace Apotheca.Api.Features.Mindmaps.GetMindmap;

public record MindmapInfo(string Id, string Name);

public class GetMindmapRepository
{
    public virtual async Task<MindmapInfo?> GetMindmapInfoAsync(IDbContext db, string projectId, string mindmapId)
    {
        return await db.QueryFirstOrDefaultAsync<MindmapInfo?>(
            @"SELECT id AS Id, name AS Name
              FROM mindmaps
              WHERE id = @MindmapId AND project_id = @ProjectId AND deleted_at IS NULL",
            new { MindmapId = mindmapId, ProjectId = projectId });
    }

    public virtual async Task<IEnumerable<GetMindmapNodeResponse>> GetNodesAsync(IDbContext db, string mindmapId)
    {
        return await db.QueryAsync<GetMindmapNodeResponse>(
            @"SELECT id             AS Id,
                     parent_node_id AS ParentNodeId,
                     header         AS Header,
                     body           AS Body,
                     collapsed      AS Collapsed,
                     position       AS Position
              FROM mindmap_nodes
              WHERE mindmap_id = @MindmapId
                AND deleted_at IS NULL
              ORDER BY position",
            new { MindmapId = mindmapId });
    }
}
