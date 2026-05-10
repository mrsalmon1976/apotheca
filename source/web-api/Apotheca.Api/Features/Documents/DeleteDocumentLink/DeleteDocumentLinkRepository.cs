using Apotheca.Data;

namespace Apotheca.Api.Features.Documents.DeleteDocumentLink;

public class DeleteDocumentLinkRepository
{
    public virtual async Task<bool> DeleteLinkAsync(IDbContext db, string projectId, string documentId, string linkId)
    {
        var rows = await db.ExecuteAsync(
            @"DELETE FROM document_links
              WHERE id = @LinkId
                AND document_id = @DocumentId
                AND document_id IN (SELECT id FROM documents WHERE project_id = @ProjectId)",
            new { LinkId = linkId, DocumentId = documentId, ProjectId = projectId });
        return rows > 0;
    }
}
