using Apotheca.Data;

namespace Apotheca.Api.Features.Mindmaps.SaveMindmapNode;

public class SaveMindmapNodeRepository
{
    private class NodeTextRow
    {
        public string Header { get; init; } = string.Empty;
        public string? Body { get; init; }
    }

    public virtual async Task<bool> NodeExistsAsync(IDbContext db, string mindmapId, string nodeId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM mindmap_nodes WHERE id = @NodeId AND mindmap_id = @MindmapId AND deleted_at IS NULL",
            new { NodeId = nodeId, MindmapId = mindmapId });
        return count > 0;
    }

    public virtual async Task UpdateNodeAsync(IDbContext db, string nodeId, string? header, string? body, bool? collapsed)
    {
        await db.ExecuteAsync(
            @"UPDATE mindmap_nodes
              SET header     = COALESCE(@Header, header),
                  body       = COALESCE(@Body, body),
                  collapsed  = COALESCE(@Collapsed, collapsed),
                  updated_at = now()
              WHERE id = @NodeId",
            new { NodeId = nodeId, Header = header, Body = body, Collapsed = collapsed });
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
