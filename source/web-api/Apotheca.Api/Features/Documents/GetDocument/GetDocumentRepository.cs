using Apotheca.Data;

namespace Apotheca.Api.Features.Documents.GetDocument;

public class GetDocumentRepository
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
        public string? Mimetype { get; init; }
        public long? FileLength { get; init; }
        public string CreatedBy { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public DateTimeOffset? DeletedAt { get; init; }
        public string? LabelsCsv { get; init; }
    }

    public virtual async Task<GetDocumentResponse?> GetDocumentAsync(
        IDbContext db, string projectId, string documentId)
    {
        var row = await db.QueryFirstOrDefaultAsync<DocumentRow>(
            @"SELECT d.id                  AS Id,
                     d.project_id          AS ProjectId,
                     d.parent_document_id  AS ParentDocumentId,
                     d.is_folder           AS IsFolder,
                     d.title               AS Title,
                     d.file_name           AS FileName,
                     d.file_extension      AS FileExtension,
                     d.mimetype            AS Mimetype,
                     d.file_length         AS FileLength,
                     d.created_by          AS CreatedBy,
                     d.created_at          AS CreatedAt,
                     d.updated_at          AS UpdatedAt,
                     d.deleted_at          AS DeletedAt,
                     string_agg(l.label_text, ',' ORDER BY l.label_text) AS LabelsCsv
              FROM documents d
              LEFT JOIN document_labels dl ON dl.document_id = d.id
              LEFT JOIN labels l           ON l.id = dl.label_id
              WHERE d.id = @DocumentId
                AND d.project_id = @ProjectId
              GROUP BY d.id",
            new { DocumentId = documentId, ProjectId = projectId });

        if (row is null) return null;

        return new GetDocumentResponse
        {
            Id               = row.Id,
            ParentDocumentId = row.ParentDocumentId,
            IsFolder         = row.IsFolder,
            Title            = row.Title,
            FileName         = row.FileName,
            FileExtension    = row.FileExtension,
            Mimetype         = row.Mimetype,
            FileLength       = row.FileLength,
            Labels           = row.LabelsCsv?.Split(',') ?? [],
            CreatedAt        = row.CreatedAt,
            UpdatedAt        = row.UpdatedAt,
            DeletedAt        = row.DeletedAt,
        };
    }
}
