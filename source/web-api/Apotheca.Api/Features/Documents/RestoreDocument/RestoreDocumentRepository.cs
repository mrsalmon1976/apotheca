using System.Text.Json;
using Apotheca.Api.Events.Documents;
using Apotheca.Data;

namespace Apotheca.Api.Features.Documents.RestoreDocument;

public record DocumentInfo(string Title, bool IsFolder);

file record AncestorRow(string Id, string Title, bool IsFolder);

public class RestoreDocumentRepository
{
    public virtual async Task<DocumentInfo?> GetDeletedDocumentInfoAsync(
        IDbContext db, string projectId, string documentId)
    {
        return await db.QueryFirstOrDefaultAsync<DocumentInfo?>(
            @"SELECT title AS Title, is_folder AS IsFolder
              FROM documents
              WHERE id = @DocumentId
                AND project_id = @ProjectId
                AND deleted_at IS NOT NULL",
            new { DocumentId = documentId, ProjectId = projectId });
    }

    public virtual async Task RestoreDocumentAsync(IDbContext db, string documentId)
    {
        await db.ExecuteAsync(
            "UPDATE documents SET deleted_at = NULL WHERE id = @DocumentId",
            new { DocumentId = documentId });
    }

    public virtual async Task<IReadOnlyList<RestoredAncestor>> RestoreAncestorsAsync(IDbContext db, string documentId)
    {
        var rows = await db.QueryAsync<AncestorRow>(
            @"WITH RECURSIVE ancestors AS (
                  SELECT id, parent_document_id FROM documents WHERE id = @DocumentId
                  UNION ALL
                  SELECT d.id, d.parent_document_id FROM documents d
                  INNER JOIN ancestors a ON d.id = a.parent_document_id
                  WHERE a.parent_document_id IS NOT NULL
              ),
              updated AS (
                  UPDATE documents SET deleted_at = NULL
                  WHERE id IN (SELECT id FROM ancestors WHERE id != @DocumentId)
                    AND deleted_at IS NOT NULL
                  RETURNING id, title, is_folder
              )
              SELECT id AS Id, title AS Title, is_folder AS IsFolder FROM updated",
            new { DocumentId = documentId });

        return rows.Select(r => new RestoredAncestor { DocumentId = r.Id, Title = r.Title, IsFolder = r.IsFolder })
                   .ToList();
    }

    public virtual async Task InsertDocumentLogAsync(
        IDbContext db, string documentId, string userId, string projectId, string title, bool isFolder)
    {
        var logMessage = isFolder ? "Document folder restored" : "Document restored";
        var newData    = JsonSerializer.Serialize(new { id = documentId, project_id = projectId, title });
        await db.ExecuteAsync(
            "INSERT INTO audit.document_logs (document_id, changed_by, operation, log_message, new_data) VALUES (@DocumentId, @ChangedBy, @Operation, @LogMessage, @NewData::jsonb)",
            new { DocumentId = documentId, ChangedBy = userId, Operation = "UPDATE", LogMessage = logMessage, NewData = newData });
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string documentId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'DOCUMENT', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = documentId, LogMessage = logMessage, UserId = userId });
    }
}
