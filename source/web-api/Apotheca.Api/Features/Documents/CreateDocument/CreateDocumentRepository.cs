using System.Text.Json;
using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.Documents.CreateDocument;

public class CreateDocumentRepository
{
    public virtual async Task<string> InsertDocumentAsync(
        IDbContext db, string projectId, string userId, string? parentDocumentId)
    {
        var id = Nanoid.Generate();
        await db.ExecuteAsync(
            @"INSERT INTO documents (id, project_id, parent_document_id, is_folder, title, created_by)
              VALUES (@Id, @ProjectId, @ParentDocumentId, FALSE, 'New Document', @CreatedBy)",
            new
            {
                Id               = id,
                ProjectId        = projectId,
                ParentDocumentId = parentDocumentId,
                CreatedBy        = userId,
            });
        return id;
    }

    public virtual async Task InsertDocumentLogAsync(
        IDbContext db, string documentId, string userId, string projectId)
    {
        var newData = JsonSerializer.Serialize(new { id = documentId, project_id = projectId });
        await db.ExecuteAsync(
            "INSERT INTO audit.document_logs (document_id, changed_by, operation, log_message, new_data) VALUES (@DocumentId, @ChangedBy, @Operation, @LogMessage, @NewData::jsonb)",
            new { DocumentId = documentId, ChangedBy = userId, Operation = "INSERT", LogMessage = "Document created", NewData = newData });
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string documentId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'DOCUMENT', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = documentId, LogMessage = logMessage, UserId = userId });
    }
}
