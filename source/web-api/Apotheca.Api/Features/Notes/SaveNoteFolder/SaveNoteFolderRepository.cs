using System.Text.Json;
using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.Notes.SaveNoteFolder;

public class SaveNoteFolderRepository
{
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

    public virtual async Task InsertNoteLogAsync(
        IDbContext db, string noteId, string userId, string projectId)
    {
        var newData = JsonSerializer.Serialize(new { id = noteId, project_id = projectId });
        await db.ExecuteAsync(
            "INSERT INTO audit.note_logs (note_id, changed_by, operation, log_message, new_data) VALUES (@NoteId, @ChangedBy, @Operation, @LogMessage, @NewData::jsonb)",
            new { NoteId = noteId, ChangedBy = userId, Operation = "INSERT", LogMessage = "Note folder created", NewData = newData });
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string folderId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'NOTE', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = folderId, LogMessage = logMessage, UserId = userId });
    }
}
