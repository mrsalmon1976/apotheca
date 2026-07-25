using Apotheca.Data;

namespace Apotheca.Api.Features.Documents.MoveDocument;

public record MoveDocumentInfo(string Title, bool IsFolder, string? ParentDocumentId);

public class MoveDocumentRepository
{
    public virtual async Task<MoveDocumentInfo?> GetDocumentInfoAsync(
        IDbContext db, string projectId, string documentId)
    {
        return await db.QueryFirstOrDefaultAsync<MoveDocumentInfo?>(
            @"SELECT title AS Title, is_folder AS IsFolder, parent_document_id AS ParentDocumentId
              FROM documents
              WHERE id = @DocumentId
                AND project_id = @ProjectId
                AND deleted_at IS NULL",
            new { DocumentId = documentId, ProjectId = projectId });
    }

    public virtual async Task<bool> TargetFolderExistsAsync(
        IDbContext db, string projectId, string folderId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM documents WHERE id = @FolderId AND project_id = @ProjectId AND is_folder = TRUE AND deleted_at IS NULL",
            new { FolderId = folderId, ProjectId = projectId });
        return count > 0;
    }

    public virtual async Task<string?> GetFolderTitleAsync(IDbContext db, string folderId)
    {
        return await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT title FROM documents WHERE id = @FolderId",
            new { FolderId = folderId });
    }

    public virtual async Task<bool> WouldCreateCycleAsync(
        IDbContext db, string documentId, string targetFolderId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            @"WITH RECURSIVE ancestors AS (
                  SELECT id, parent_document_id FROM documents WHERE id = @TargetFolderId
                  UNION ALL
                  SELECT d.id, d.parent_document_id
                  FROM documents d
                  JOIN ancestors a ON d.id = a.parent_document_id
              )
              SELECT COUNT(1) FROM ancestors WHERE id = @DocumentId",
            new { DocumentId = documentId, TargetFolderId = targetFolderId });
        return count > 0;
    }

    public virtual async Task MoveDocumentAsync(
        IDbContext db, string projectId, string documentId, string? targetFolderId)
    {
        await db.ExecuteAsync(
            @"UPDATE documents SET parent_document_id = @TargetFolderId, updated_at = now()
              WHERE id = @DocumentId AND project_id = @ProjectId",
            new { DocumentId = documentId, ProjectId = projectId, TargetFolderId = targetFolderId });
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string documentId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'DOCUMENT', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = documentId, LogMessage = logMessage, UserId = userId });
    }
}
