using Apotheca.Data;

namespace Apotheca.Api.Features.Notes.GetNoteAttachment;

public class GetNoteAttachmentRepository
{
    public virtual async Task<NoteAttachment?> GetAttachmentAsync(
        IDbContext db, string projectId, string attachmentId)
    {
        return await db.QueryFirstOrDefaultAsync<NoteAttachment>(
            @"SELECT id, blob_reference AS BlobReference, file_name AS FileName, mimetype
              FROM note_attachments
              WHERE id = @AttachmentId
                AND project_id = @ProjectId
                AND deleted_at IS NULL",
            new { AttachmentId = attachmentId, ProjectId = projectId });
    }

    public record NoteAttachment(string Id, string BlobReference, string FileName, string Mimetype);
}
