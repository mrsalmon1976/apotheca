using Apotheca.Data;

namespace Apotheca.Api.Features.Documents.DownloadDocumentByLink;

public class DownloadDocumentByLinkRepository
{
    private class DownloadRow
    {
        public string? BlobReference { get; init; }
        public string? FileName { get; init; }
        public string? Mimetype { get; init; }
    }

    public virtual async Task<(string BlobReference, string FileName, string Mimetype)?> GetDownloadInfoAsync(
        IDbContext db, string linkId)
    {
        var row = await db.QueryFirstOrDefaultAsync<DownloadRow>(
            @"SELECT d.blob_reference AS BlobReference,
                     d.file_name      AS FileName,
                     d.mimetype       AS Mimetype
              FROM document_links dl
              JOIN documents d ON d.id = dl.document_id
              WHERE dl.id       = @LinkId
                AND d.deleted_at IS NULL
                AND d.is_folder  = FALSE",
            new { LinkId = linkId });

        if (row?.BlobReference is null || row.FileName is null)
            return null;

        return (row.BlobReference, row.FileName, row.Mimetype ?? "application/octet-stream");
    }
}
