using System.Text.Json;
using Apotheca.Data;

namespace Apotheca.Api.Features.Documents.UploadDocument;

public class UploadDocumentRepository
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

    public virtual async Task<string> InsertDocumentWithIdAsync(
        IDbContext db, string id, string projectId, string userId, string? parentDocumentId,
        string title, string fileName, string fileExtension, string mimetype,
        long fileLength, string blobReference)
    {
        await db.ExecuteAsync(
            @"INSERT INTO documents
                (id, project_id, parent_document_id, is_folder, title,
                 file_name, file_extension, mimetype, file_length, blob_reference, created_by)
              VALUES
                (@Id, @ProjectId, @ParentDocumentId, FALSE, @Title,
                 @FileName, @FileExtension, @Mimetype, @FileLength, @BlobReference, @CreatedBy)",
            new
            {
                Id               = id,
                ProjectId        = projectId,
                ParentDocumentId = parentDocumentId,
                Title            = title,
                FileName         = fileName,
                FileExtension    = fileExtension,
                Mimetype         = mimetype,
                FileLength       = fileLength,
                BlobReference    = blobReference,
                CreatedBy        = userId,
            });
        return id;
    }


    public virtual async Task InsertDocumentLogAsync(
        IDbContext db, string documentId, string userId, string projectId)
    {
        var newData = JsonSerializer.Serialize(new { id = documentId, project_id = projectId });
        await db.ExecuteAsync(
            @"INSERT INTO audit.document_logs (document_id, changed_by, operation, log_message, new_data)
              VALUES (@DocumentId, @ChangedBy, @Operation, @LogMessage, @NewData::jsonb)",
            new { DocumentId = documentId, ChangedBy = userId, Operation = "INSERT", LogMessage = "Document uploaded", NewData = newData });
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string documentId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            @"INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id)
              VALUES (@ProjectId, @RefId, 'DOCUMENT', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = documentId, LogMessage = logMessage, UserId = userId });
    }
}
