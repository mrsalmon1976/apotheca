using Apotheca.Data;

namespace Apotheca.Api.Features.Notes.SaveNoteAttachment;

public class SaveNoteAttachmentRepository
{
    public virtual async Task InsertNoteAttachmentAsync(
        IDbContext db, string id, string projectId, string noteId,
        string blobReference, string fileName, string mimetype,
        long fileLength, string createdBy)
    {
        await db.ExecuteAsync(
            @"INSERT INTO note_attachments
                (id, project_id, note_id, blob_reference, file_name, mimetype, file_length, created_by)
              VALUES
                (@Id, @ProjectId, @NoteId, @BlobReference, @FileName, @Mimetype, @FileLength, @CreatedBy)",
            new
            {
                Id             = id,
                ProjectId      = projectId,
                NoteId         = noteId,
                BlobReference  = blobReference,
                FileName       = fileName,
                Mimetype       = mimetype,
                FileLength     = fileLength,
                CreatedBy      = createdBy,
            });
    }

    public virtual async Task<bool> NoteExistsAsync(IDbContext db, string projectId, string noteId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM notes WHERE id = @NoteId AND project_id = @ProjectId AND is_folder = FALSE",
            new { NoteId = noteId, ProjectId = projectId });
        return count > 0;
    }
}
