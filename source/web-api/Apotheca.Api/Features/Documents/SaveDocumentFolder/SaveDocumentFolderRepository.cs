using System.Text.Json;
using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.Documents.SaveDocumentFolder;

public class SaveDocumentFolderRepository
{
    public virtual async Task<string> InsertDocumentFolderAsync(
        IDbContext db, string projectId, string userId, string title, string? parentDocumentId)
    {
        var id = Nanoid.Generate();
        await db.ExecuteAsync(
            @"INSERT INTO documents (id, project_id, parent_document_id, is_folder, title, created_by)
              VALUES (@Id, @ProjectId, @ParentDocumentId, TRUE, @Title, @CreatedBy)",
            new
            {
                Id               = id,
                ProjectId        = projectId,
                ParentDocumentId = parentDocumentId,
                Title            = title,
                CreatedBy        = userId,
            });
        return id;
    }

    public virtual async Task<string> UpsertLabelAsync(
        IDbContext db, string projectId, string userId, string labelText)
    {
        await db.ExecuteAsync(
            @"INSERT INTO labels (id, project_id, label_text, created_by)
              VALUES (@Id, @ProjectId, @LabelText, @CreatedBy)
              ON CONFLICT (project_id, label_text) DO NOTHING",
            new
            {
                Id        = Nanoid.Generate(),
                ProjectId = projectId,
                LabelText = labelText,
                CreatedBy = userId,
            });

        return (await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT id FROM labels WHERE project_id = @ProjectId AND label_text = @LabelText",
            new { ProjectId = projectId, LabelText = labelText }))!;
    }

    public virtual async Task InsertDocumentLabelAsync(IDbContext db, string documentId, string labelId)
    {
        await db.ExecuteAsync(
            "INSERT INTO document_labels (document_id, label_id) VALUES (@DocumentId, @LabelId) ON CONFLICT DO NOTHING",
            new { DocumentId = documentId, LabelId = labelId });
    }

    public virtual async Task InsertDocumentLogAsync(
        IDbContext db, string documentId, string userId, string projectId)
    {
        var newData = JsonSerializer.Serialize(new { id = documentId, project_id = projectId });
        await db.ExecuteAsync(
            "INSERT INTO audit.document_logs (document_id, changed_by, operation, log_message, new_data) VALUES (@DocumentId, @ChangedBy, @Operation, @LogMessage, @NewData::jsonb)",
            new { DocumentId = documentId, ChangedBy = userId, Operation = "INSERT", LogMessage = "Document folder created", NewData = newData });
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string folderId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'DOCUMENT', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = folderId, LogMessage = logMessage, UserId = userId });
    }
}
