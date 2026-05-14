using Apotheca.Data;

namespace Apotheca.Api.Events.Documents.DocumentDeleted;

public record DeletedDescendant(string Id, string Title, bool IsFolder);

public class DocumentDeletedRepository
{
    public virtual async Task<IReadOnlyList<DeletedDescendant>> SoftDeleteDescendantsAsync(
        IDbContext db, string documentId)
    {
        var rows = await db.QueryAsync<DeletedDescendant>(
            @"WITH RECURSIVE descendants AS (
                  SELECT id, is_folder FROM documents
                  WHERE parent_document_id = @DocumentId AND deleted_at IS NULL
                  UNION ALL
                  SELECT d.id, d.is_folder FROM documents d
                  INNER JOIN descendants anc ON d.parent_document_id = anc.id
                  WHERE d.deleted_at IS NULL
              ),
              updated AS (
                  UPDATE documents SET deleted_at = now() AT TIME ZONE 'UTC'
                  WHERE id IN (SELECT id FROM descendants)
                  RETURNING id, title, is_folder
              )
              SELECT id AS Id, title AS Title, is_folder AS IsFolder FROM updated",
            new { DocumentId = documentId });

        return rows.ToList();
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string documentId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'DOCUMENT', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = documentId, LogMessage = logMessage, UserId = userId });
    }
}
