using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.Mindmaps.CreateMindmap;

public class CreateMindmapRepository
{
    public virtual async Task<string> InsertMindmapAsync(IDbContext db, string projectId, string userId, string name)
    {
        var id = Nanoid.Generate();
        await db.ExecuteAsync(
            @"INSERT INTO mindmaps (id, project_id, name, created_by)
              VALUES (@Id, @ProjectId, @Name, @CreatedBy)",
            new { Id = id, ProjectId = projectId, Name = name, CreatedBy = userId });
        return id;
    }

    public virtual async Task<string> InsertRootNodeAsync(IDbContext db, string mindmapId, string userId)
    {
        var id = Nanoid.Generate();
        await db.ExecuteAsync(
            @"INSERT INTO mindmap_nodes (id, mindmap_id, parent_node_id, header, body, position, created_by)
              VALUES (@Id, @MindmapId, NULL, 'Central Idea', '', 0, @CreatedBy)",
            new { Id = id, MindmapId = mindmapId, CreatedBy = userId });
        return id;
    }

    public virtual async Task UpsertSearchAsync(IDbContext db, string projectId, string mindmapId, string title, string body)
    {
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
}
