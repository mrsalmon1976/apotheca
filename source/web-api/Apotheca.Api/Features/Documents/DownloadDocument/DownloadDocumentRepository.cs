using Apotheca.Data;

namespace Apotheca.Api.Features.Documents.DownloadDocument;

public class DownloadDocumentRepository
{
    private class DocumentRow
    {
        public string? BlobReference { get; init; }
        public string? FileName { get; init; }
        public string? Mimetype { get; init; }
    }

    public virtual async Task<(string BlobReference, string FileName, string Mimetype)?> GetDownloadInfoAsync(
        IDbContext db, string projectId, string documentId)
    {
        var row = await db.QueryFirstOrDefaultAsync<DocumentRow>(
            @"SELECT blob_reference AS BlobReference,
                     file_name      AS FileName,
                     mimetype       AS Mimetype
              FROM documents
              WHERE id         = @DocumentId
                AND project_id = @ProjectId
                AND is_folder  = FALSE
                AND deleted_at IS NULL",
            new { DocumentId = documentId, ProjectId = projectId });

        if (row?.BlobReference is null || row.FileName is null)
            return null;

        return (row.BlobReference, row.FileName, row.Mimetype ?? "application/octet-stream");
    }
}
