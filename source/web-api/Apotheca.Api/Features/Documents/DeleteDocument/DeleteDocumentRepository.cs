using System.Text.Json;
using Apotheca.Data;

namespace Apotheca.Api.Features.Documents.DeleteDocument;

public record DocumentInfo(string Title, bool IsFolder);

public class DeleteDocumentRepository
{
    public virtual async Task<bool> UserHasProjectAccessAsync(
        IDbContext db, string firebaseUid, string projectId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            @"SELECT COUNT(1)
              FROM user_projects up
              INNER JOIN user_firebase_identities ufi ON ufi.user_id = up.user_id
              WHERE ufi.firebase_uid = @FirebaseUid
                AND up.project_id = @ProjectId",
            new { FirebaseUid = firebaseUid, ProjectId = projectId });
        return count > 0;
    }

    public virtual async Task<string?> GetUserIdAsync(IDbContext db, string firebaseUid)
    {
        return await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT user_id FROM user_firebase_identities WHERE firebase_uid = @FirebaseUid",
            new { FirebaseUid = firebaseUid });
    }

    public virtual async Task<DocumentInfo?> GetDocumentInfoAsync(
        IDbContext db, string projectId, string documentId)
    {
        return await db.QueryFirstOrDefaultAsync<DocumentInfo?>(
            @"SELECT title AS Title, is_folder AS IsFolder
              FROM documents
              WHERE id = @DocumentId
                AND project_id = @ProjectId
                AND deleted_at IS NULL",
            new { DocumentId = documentId, ProjectId = projectId });
    }

    public virtual async Task SoftDeleteDocumentAsync(IDbContext db, string documentId)
    {
        await db.ExecuteAsync(
            "UPDATE documents SET deleted_at = now() AT TIME ZONE 'UTC' WHERE id = @DocumentId AND deleted_at IS NULL",
            new { DocumentId = documentId });
    }

    public virtual async Task InsertDocumentLogAsync(
        IDbContext db, string documentId, string userId, string projectId, string title, bool isFolder)
    {
        var logMessage = isFolder ? "Document folder deleted" : "Document deleted";
        var oldData    = JsonSerializer.Serialize(new { id = documentId, project_id = projectId, title });
        await db.ExecuteAsync(
            "INSERT INTO audit.document_logs (document_id, changed_by, operation, log_message, old_data) VALUES (@DocumentId, @ChangedBy, @Operation, @LogMessage, @OldData::jsonb)",
            new { DocumentId = documentId, ChangedBy = userId, Operation = "DELETE", LogMessage = logMessage, OldData = oldData });
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string documentId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'DOCUMENT', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = documentId, LogMessage = logMessage, UserId = userId });
    }
}
