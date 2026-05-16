using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.Documents.SaveDocument;

public class SaveDocumentRepository
{
    public virtual async Task DeleteDocumentLabelsAsync(IDbContext db, string documentId)
    {
        await db.ExecuteAsync(
            "DELETE FROM document_labels WHERE document_id = @DocumentId",
            new { DocumentId = documentId });
    }

    public virtual async Task<bool> DocumentExistsAsync(IDbContext db, string projectId, string documentId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM documents WHERE id = @DocumentId AND project_id = @ProjectId AND is_folder = FALSE",
            new { DocumentId = documentId, ProjectId = projectId });
        return count > 0;
    }

    public virtual async Task InsertDocumentLabelAsync(IDbContext db, string documentId, string labelId)
    {
        await db.ExecuteAsync(
            "INSERT INTO document_labels (document_id, label_id) VALUES (@DocumentId, @LabelId) ON CONFLICT DO NOTHING",
            new { DocumentId = documentId, LabelId = labelId });
    }

    public virtual async Task UpdateDocumentTitleAsync(
        IDbContext db, string projectId, string documentId, string title)
    {
        await db.ExecuteAsync(
            @"UPDATE documents
              SET title      = @Title,
                  updated_at = now()
              WHERE id = @DocumentId
                AND project_id = @ProjectId",
            new { Title = title, DocumentId = documentId, ProjectId = projectId });
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

    public virtual async Task UpsertSearchAsync(IDbContext db, string projectId, string documentId, string title)
    {
        await db.ExecuteAsync(
            @"INSERT INTO search (reference_id, reference_type, project_id, text_title, text_body, updated_at)
              VALUES (@ReferenceId, 'document', @ProjectId, @Title, '', now())
              ON CONFLICT (reference_id, reference_type) DO UPDATE
              SET project_id = EXCLUDED.project_id,
                  text_title = EXCLUDED.text_title,
                  updated_at = now()",
            new { ReferenceId = documentId, ProjectId = projectId, Title = title });
    }
}
