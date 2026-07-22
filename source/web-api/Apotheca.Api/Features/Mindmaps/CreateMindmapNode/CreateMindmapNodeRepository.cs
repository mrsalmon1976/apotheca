using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.Mindmaps.CreateMindmapNode;

public class CreateMindmapNodeRepository
{
    private class NodeTextRow
    {
        public string Header { get; init; } = string.Empty;
        public string? Body { get; init; }
    }

    public virtual async Task<int> GetNextPositionAsync(IDbContext db, string mindmapId, string parentNodeId)
    {
        var maxPosition = await db.QueryFirstOrDefaultAsync<int?>(
            @"SELECT MAX(position) FROM mindmap_nodes
              WHERE mindmap_id = @MindmapId AND parent_node_id = @ParentNodeId AND deleted_at IS NULL",
            new { MindmapId = mindmapId, ParentNodeId = parentNodeId });
        return (maxPosition ?? -1) + 1;
    }

    public virtual async Task<string> InsertNodeAsync(
        IDbContext db, string mindmapId, string parentNodeId, string userId, string header, string body, int position)
    {
        var id = Nanoid.Generate();
        await db.ExecuteAsync(
            @"INSERT INTO mindmap_nodes (id, mindmap_id, parent_node_id, header, body, position, created_by)
              VALUES (@Id, @MindmapId, @ParentNodeId, @Header, @Body, @Position, @CreatedBy)",
            new
            {
                Id = id,
                MindmapId = mindmapId,
                ParentNodeId = parentNodeId,
                Header = header,
                Body = body,
                Position = position,
                CreatedBy = userId,
            });
        return id;
    }

    public virtual async Task<bool> MindmapExistsAsync(IDbContext db, string projectId, string mindmapId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM mindmaps WHERE id = @MindmapId AND project_id = @ProjectId AND deleted_at IS NULL",
            new { MindmapId = mindmapId, ProjectId = projectId });
        return count > 0;
    }

    public virtual async Task<bool> NodeExistsAsync(IDbContext db, string mindmapId, string nodeId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM mindmap_nodes WHERE id = @NodeId AND mindmap_id = @MindmapId AND deleted_at IS NULL",
            new { NodeId = nodeId, MindmapId = mindmapId });
        return count > 0;
    }

    public virtual async Task RecomputeSearchAsync(IDbContext db, string projectId, string mindmapId)
    {
        var title = await db.QueryFirstOrDefaultAsync<string>(
            "SELECT name FROM mindmaps WHERE id = @MindmapId", new { MindmapId = mindmapId }) ?? "Untitled Mindmap";

        var nodes = (await db.QueryAsync<NodeTextRow>(
            @"SELECT header AS Header, body AS Body
              FROM mindmap_nodes
              WHERE mindmap_id = @MindmapId AND deleted_at IS NULL
              ORDER BY position",
            new { MindmapId = mindmapId })).ToList();

        var body = string.Join(" ", nodes
            .Select(n => $"{n.Header} {n.Body}".Trim())
            .Where(s => s.Length > 0));

        await db.ExecuteAsync(
            @"INSERT INTO search (reference_id, reference_type, project_id, text_title, text_body, updated_at)
              VALUES (@ReferenceId, 'mindmap', @ProjectId, @Title, @Body, now())
              ON CONFLICT (reference_id, reference_type) DO UPDATE
              SET project_id = EXCLUDED.project_id,
                  text_title = EXCLUDED.text_title,
                  text_body  = EXCLUDED.text_body,
                  updated_at = now()",
            new { ReferenceId = mindmapId, ProjectId = projectId, Title = title, Body = body });
    }

    public virtual async Task TouchMindmapAsync(IDbContext db, string mindmapId)
    {
        await db.ExecuteAsync(
            "UPDATE mindmaps SET updated_at = now() WHERE id = @MindmapId",
            new { MindmapId = mindmapId });
    }
}
