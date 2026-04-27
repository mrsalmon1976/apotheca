using System.Text.Json;
using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.Notes.CreateNote;

public class CreateNoteRepository
{
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

    public virtual async Task InsertNoteLogAsync(
        IDbContext db, string noteId, string userId, string projectId)
    {
        var newData = JsonSerializer.Serialize(new { id = noteId, project_id = projectId });
        await db.ExecuteAsync(
            "INSERT INTO audit.note_logs (note_id, changed_by, operation, log_message, new_data) VALUES (@NoteId, @ChangedBy, @Operation, @LogMessage, @NewData::jsonb)",
            new { NoteId = noteId, ChangedBy = userId, Operation = "INSERT", LogMessage = "Note created", NewData = newData });
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string noteId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'NOTE', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = noteId, LogMessage = logMessage, UserId = userId });
    }
}
