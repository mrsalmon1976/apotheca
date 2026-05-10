using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.Documents.CreateDocumentLink;

public class CreateDocumentLinkRepository
{
    public virtual async Task<bool> DocumentExistsAsync(IDbContext db, string projectId, string documentId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            @"SELECT COUNT(1)
              FROM documents
              WHERE id         = @DocumentId
                AND project_id = @ProjectId
                AND is_folder  = FALSE
                AND deleted_at IS NULL",
            new { DocumentId = documentId, ProjectId = projectId });
        return count > 0;
    }

    public virtual async Task<CreateDocumentLinkResponse> InsertLinkAsync(
        IDbContext db, string documentId, string userId)
    {
        var id = Nanoid.Generate(size: 48);
        var row = await db.QueryFirstOrDefaultAsync<CreateDocumentLinkResponse>(
            @"INSERT INTO document_links (id, document_id, created_by)
              VALUES (@Id, @DocumentId, @CreatedBy)
              RETURNING id AS Id, created_at AS CreatedAt",
            new { Id = id, DocumentId = documentId, CreatedBy = userId });
        return row!;
    }
}
