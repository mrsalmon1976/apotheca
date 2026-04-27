using Apotheca.Data;

namespace Apotheca.Api.Features.Documents.GetDocuments;

public class GetDocumentsRepository
{
    private class DocumentRow
    {
        public string Id { get; init; } = string.Empty;
        public string ProjectId { get; init; } = string.Empty;
        public string? ParentDocumentId { get; init; }
        public bool IsFolder { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? FileName { get; init; }
        public string? FileExtension { get; init; }
        public long? FileLength { get; init; }
        public string CreatedBy { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public string? LabelsCsv { get; init; }
    }

    public virtual async Task<IEnumerable<GetDocumentsResponse>> GetDocumentsAsync(
        IDbContext db, string projectId, string? parentDocumentId)
    {
        IEnumerable<DocumentRow> rows;

        if (parentDocumentId is null)
        {
            rows = await db.QueryAsync<DocumentRow>(
                @"SELECT d.id                  AS Id,
                         d.project_id          AS ProjectId,
                         d.parent_document_id  AS ParentDocumentId,
                         d.is_folder           AS IsFolder,
                         d.title               AS Title,
                         d.file_name           AS FileName,
                         d.file_extension      AS FileExtension,
                         d.file_length         AS FileLength,
                         d.created_by          AS CreatedBy,
                         d.created_at          AS CreatedAt,
                         d.updated_at          AS UpdatedAt,
                         string_agg(l.label_text, ',' ORDER BY l.label_text) AS LabelsCsv
                  FROM documents d
                  LEFT JOIN document_labels dl ON dl.document_id = d.id
                  LEFT JOIN labels l           ON l.id = dl.label_id
                  WHERE d.project_id = @ProjectId
                    AND d.parent_document_id IS NULL
                    AND d.deleted_at IS NULL
                  GROUP BY d.id
                  ORDER BY d.is_folder DESC, lower(d.title)",
                new { ProjectId = projectId });
        }
        else
        {
            rows = await db.QueryAsync<DocumentRow>(
                @"SELECT d.id                  AS Id,
                         d.project_id          AS ProjectId,
                         d.parent_document_id  AS ParentDocumentId,
                         d.is_folder           AS IsFolder,
                         d.title               AS Title,
                         d.file_name           AS FileName,
                         d.file_extension      AS FileExtension,
                         d.file_length         AS FileLength,
                         d.created_by          AS CreatedBy,
                         d.created_at          AS CreatedAt,
                         d.updated_at          AS UpdatedAt,
                         string_agg(l.label_text, ',' ORDER BY l.label_text) AS LabelsCsv
                  FROM documents d
                  LEFT JOIN document_labels dl ON dl.document_id = d.id
                  LEFT JOIN labels l           ON l.id = dl.label_id
                  WHERE d.project_id = @ProjectId
                    AND d.parent_document_id = @ParentDocumentId
                    AND d.deleted_at IS NULL
                  GROUP BY d.id
                  ORDER BY d.is_folder DESC, lower(d.title)",
                new { ProjectId = projectId, ParentDocumentId = parentDocumentId });
        }

        return rows.Select(r => new GetDocumentsResponse
        {
            Id               = r.Id,
            ParentDocumentId = r.ParentDocumentId,
            IsFolder         = r.IsFolder,
            Title            = r.Title,
            FileName         = r.FileName,
            FileExtension    = r.FileExtension,
            FileLength       = r.FileLength,
            Labels           = r.LabelsCsv?.Split(',') ?? [],
            CreatedAt        = r.CreatedAt,
            UpdatedAt        = r.UpdatedAt,
        });
    }
}
