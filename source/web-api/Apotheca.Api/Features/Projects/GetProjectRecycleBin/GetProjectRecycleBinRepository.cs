using Apotheca.Data;

namespace Apotheca.Api.Features.Projects.GetProjectRecycleBin;

public class GetProjectRecycleBinRepository
{
    private class RecycleBinRow
    {
        public string Id { get; init; } = string.Empty;
        public bool IsFolder { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? DeletedBy { get; init; }
        public DateTimeOffset DeletedAt { get; init; }
    }

    public virtual async Task<IEnumerable<GetProjectRecycleBinResponse>> GetDeletedNotesAsync(
        IDbContext db, string projectId)
    {
        var rows = await db.QueryAsync<RecycleBinRow>(
            @"SELECT n.id         AS Id,
                     n.is_folder  AS IsFolder,
                     n.title      AS Title,
                     n.deleted_at AS DeletedAt,
                     u.display_name AS DeletedBy
              FROM notes n
              LEFT JOIN LATERAL (
                  SELECT changed_by
                  FROM audit.note_logs
                  WHERE note_id = n.id AND operation = 'DELETE'
                  ORDER BY changed_at DESC
                  LIMIT 1
              ) last_delete ON true
              LEFT JOIN users u ON u.id = last_delete.changed_by
              WHERE n.project_id = @ProjectId
                AND n.deleted_at IS NOT NULL
              ORDER BY n.deleted_at DESC",
            new { ProjectId = projectId });

        return rows.Select(r => new GetProjectRecycleBinResponse
        {
            Id        = r.Id,
            Type      = r.IsFolder ? "FOLDER" : "NOTE",
            Title     = r.Title,
            DeletedBy = r.DeletedBy,
            DeletedAt = r.DeletedAt,
        });
    }

    public virtual async Task<IEnumerable<GetProjectRecycleBinResponse>> GetDeletedDocumentsAsync(
        IDbContext db, string projectId)
    {
        var rows = await db.QueryAsync<RecycleBinRow>(
            @"SELECT d.id         AS Id,
                     d.is_folder  AS IsFolder,
                     d.title      AS Title,
                     d.deleted_at AS DeletedAt,
                     u.display_name AS DeletedBy
              FROM documents d
              LEFT JOIN LATERAL (
                  SELECT changed_by
                  FROM audit.document_logs
                  WHERE document_id = d.id AND operation = 'DELETE'
                  ORDER BY changed_at DESC
                  LIMIT 1
              ) last_delete ON true
              LEFT JOIN users u ON u.id = last_delete.changed_by
              WHERE d.project_id = @ProjectId
                AND d.deleted_at IS NOT NULL
              ORDER BY d.deleted_at DESC",
            new { ProjectId = projectId });

        return rows.Select(r => new GetProjectRecycleBinResponse
        {
            Id        = r.Id,
            Type      = r.IsFolder ? "FOLDER" : "DOCUMENT",
            Title     = r.Title,
            DeletedBy = r.DeletedBy,
            DeletedAt = r.DeletedAt,
        });
    }
}
