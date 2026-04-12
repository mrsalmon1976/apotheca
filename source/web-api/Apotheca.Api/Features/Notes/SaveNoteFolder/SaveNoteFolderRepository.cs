using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.Notes.SaveNoteFolder;

public class SaveNoteFolderRepository
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

    public virtual async Task<string> InsertNoteFolderAsync(
        IDbContext db, string projectId, string userId, string title, string? parentNoteId)
    {
        var id = Nanoid.Generate();
        await db.ExecuteAsync(
            @"INSERT INTO notes (id, project_id, parent_note_id, is_folder, title, created_by)
              VALUES (@Id, @ProjectId, @ParentNoteId, TRUE, @Title, @CreatedBy)",
            new
            {
                Id           = id,
                ProjectId    = projectId,
                ParentNoteId = parentNoteId,
                Title        = title,
                CreatedBy    = userId,
            });
        return id;
    }

    public virtual async Task<string> UpsertLabelAsync(
        IDbContext db, string projectId, string userId, string labelText)
    {
        await db.ExecuteAsync(
            @"INSERT INTO labels (id, project_id, label_text, created_by)
              VALUES (@Id, @ProjectId, @LabelText, @CreatedBy)
              ON CONFLICT (project_id, label_text) DO NOTHING",
            new
            {
                Id        = Nanoid.Generate(),
                ProjectId = projectId,
                LabelText = labelText,
                CreatedBy = userId,
            });

        return (await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT id FROM labels WHERE project_id = @ProjectId AND label_text = @LabelText",
            new { ProjectId = projectId, LabelText = labelText }))!;
    }

    public virtual async Task InsertNoteLabelAsync(IDbContext db, string noteId, string labelId)
    {
        await db.ExecuteAsync(
            "INSERT INTO note_labels (note_id, label_id) VALUES (@NoteId, @LabelId) ON CONFLICT DO NOTHING",
            new { NoteId = noteId, LabelId = labelId });
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string folderId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'NOTE', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = folderId, LogMessage = logMessage, UserId = userId });
    }
}
