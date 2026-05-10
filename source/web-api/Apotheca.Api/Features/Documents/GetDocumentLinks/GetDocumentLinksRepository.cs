using Apotheca.Data;

namespace Apotheca.Api.Features.Documents.GetDocumentLinks;

public class GetDocumentLinksRepository
{
    public virtual async Task<IEnumerable<GetDocumentLinksResponse>> GetLinksAsync(
        IDbContext db, string projectId, string documentId)
    {
        return await db.QueryAsync<GetDocumentLinksResponse>(
            @"SELECT dl.id         AS Id,
                     dl.created_at AS CreatedAt
              FROM document_links dl
              JOIN documents d ON d.id = dl.document_id
              WHERE dl.document_id = @DocumentId
                AND d.project_id   = @ProjectId
              ORDER BY dl.created_at DESC",
            new { DocumentId = documentId, ProjectId = projectId });
    }
}
