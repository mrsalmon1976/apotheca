using Apotheca.Data;

namespace Apotheca.Api.Events.Documents.DocumentUploaded;

public class DocumentUploadedEventRepository
{
    public virtual async Task UpdateSearchBodyAsync(IDbContext db, string documentId, string body)
    {
        await db.ExecuteAsync(
            @"UPDATE search
              SET text_body  = @Body,
                  updated_at = now()
              WHERE reference_id   = @ReferenceId
                AND reference_type = 'document'",
            new { ReferenceId = documentId, Body = body });
    }
}
