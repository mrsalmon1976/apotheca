using Apotheca.Data;
using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Notes.GetNote;

public class GetNoteRepository
{
    public virtual async Task<bool> UserHasProjectAccessAsync(
        IDbContext db, string firebaseUid, string projectId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            @"SELECT COUNT(1)
              FROM user_projects up
              INNER JOIN user_firebase_identities ufi ON ufi.user_id = up.user_id
              WHERE ufi.firebase_uid = @FirebaseUid
                AND up.project_id = @ProjectId",
            new { FirebaseUid = firebaseUid, ProjectId = projectId });
        return count > 0;
    }

    public virtual async Task<NoteDbEntity?> GetNoteAsync(
        IDbContext db, string projectId, string noteId)
    {
        return await db.QueryFirstOrDefaultAsync<NoteDbEntity>(
            @"SELECT id             AS Id,
                     project_id     AS ProjectId,
                     parent_note_id AS ParentNoteId,
                     is_folder      AS IsFolder,
                     title          AS Title,
                     body           AS Body,
                     created_by     AS CreatedBy,
                     created_at     AS CreatedAt,
                     updated_at     AS UpdatedAt
              FROM notes
              WHERE id = @NoteId
                AND project_id = @ProjectId",
            new { NoteId = noteId, ProjectId = projectId });
    }
}
