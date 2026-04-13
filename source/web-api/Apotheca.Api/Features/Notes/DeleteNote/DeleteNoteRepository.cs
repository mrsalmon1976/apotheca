using System.Text.Json;
using Apotheca.Data;

namespace Apotheca.Api.Features.Notes.DeleteNote;

public record NoteInfo(string Title, bool IsFolder);

public class DeleteNoteRepository
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

    public virtual async Task<NoteInfo?> GetNoteInfoAsync(
        IDbContext db, string projectId, string noteId)
    {
        return await db.QueryFirstOrDefaultAsync<NoteInfo?>(
            @"SELECT title AS Title, is_folder AS IsFolder
              FROM notes
              WHERE id = @NoteId
                AND project_id = @ProjectId
                AND deleted_at IS NULL",
            new { NoteId = noteId, ProjectId = projectId });
    }

    public virtual async Task SoftDeleteNoteAsync(IDbContext db, string noteId)
    {
        await db.ExecuteAsync(
            "UPDATE notes SET deleted_at = now() AT TIME ZONE 'UTC' WHERE id = @NoteId AND deleted_at IS NULL",
            new { NoteId = noteId });
    }

    public virtual async Task InsertNoteLogAsync(
        IDbContext db, string noteId, string userId, string projectId, string title, bool isFolder)
    {
        var logMessage = isFolder ? "Note folder deleted" : "Note deleted";
        var oldData    = JsonSerializer.Serialize(new { id = noteId, project_id = projectId, title });
        await db.ExecuteAsync(
            "INSERT INTO audit.note_logs (note_id, changed_by, operation, log_message, old_data) VALUES (@NoteId, @ChangedBy, @Operation, @LogMessage, @OldData::jsonb)",
            new { NoteId = noteId, ChangedBy = userId, Operation = "DELETE", LogMessage = logMessage, OldData = oldData });
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string noteId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'NOTE', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = noteId, LogMessage = logMessage, UserId = userId });
    }
}
