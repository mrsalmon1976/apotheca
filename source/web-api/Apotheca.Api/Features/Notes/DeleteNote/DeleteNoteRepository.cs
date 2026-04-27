using System.Text.Json;
using Apotheca.Data;

namespace Apotheca.Api.Features.Notes.DeleteNote;

public record NoteInfo(string Title, bool IsFolder);

public class DeleteNoteRepository
{
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
