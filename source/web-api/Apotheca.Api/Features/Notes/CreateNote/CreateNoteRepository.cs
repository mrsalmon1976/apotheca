using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.Notes.CreateNote;

public class CreateNoteRepository
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

    public virtual async Task<string?> GetUserIdAsync(IDbContext db, string firebaseUid)
    {
        return await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT user_id FROM user_firebase_identities WHERE firebase_uid = @FirebaseUid",
            new { FirebaseUid = firebaseUid });
    }

    public virtual async Task<string> InsertNoteAsync(
        IDbContext db, string projectId, string userId, string? parentNoteId)
    {
        var id = Nanoid.Generate();
        await db.ExecuteAsync(
            @"INSERT INTO notes (id, project_id, parent_note_id, is_folder, title, created_by)
              VALUES (@Id, @ProjectId, @ParentNoteId, FALSE, 'New Note', @CreatedBy)",
            new
            {
                Id           = id,
                ProjectId    = projectId,
                ParentNoteId = parentNoteId,
                CreatedBy    = userId,
            });
        return id;
    }
}
