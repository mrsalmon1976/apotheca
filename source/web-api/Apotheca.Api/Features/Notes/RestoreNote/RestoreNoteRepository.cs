using System.Text.Json;
using Apotheca.Api.Events.Notes;
using Apotheca.Data;

namespace Apotheca.Api.Features.Notes.RestoreNote;

public record NoteInfo(string Title, bool IsFolder);

file record AncestorRow(string Id, string Title, bool IsFolder);

public class RestoreNoteRepository
{
    public virtual async Task<NoteInfo?> GetDeletedNoteInfoAsync(
        IDbContext db, string projectId, string noteId)
    {
        return await db.QueryFirstOrDefaultAsync<NoteInfo?>(
            @"SELECT title AS Title, is_folder AS IsFolder
              FROM notes
              WHERE id = @NoteId
                AND project_id = @ProjectId
                AND deleted_at IS NOT NULL",
            new { NoteId = noteId, ProjectId = projectId });
    }

    public virtual async Task RestoreNoteAsync(IDbContext db, string noteId)
    {
        await db.ExecuteAsync(
            "UPDATE notes SET deleted_at = NULL WHERE id = @NoteId",
            new { NoteId = noteId });
    }

    public virtual async Task<IReadOnlyList<RestoredAncestor>> RestoreAncestorsAsync(IDbContext db, string noteId)
    {
        var rows = await db.QueryAsync<AncestorRow>(
            @"WITH RECURSIVE ancestors AS (
                  SELECT id, parent_note_id FROM notes WHERE id = @NoteId
                  UNION ALL
                  SELECT n.id, n.parent_note_id FROM notes n
                  INNER JOIN ancestors a ON n.id = a.parent_note_id
                  WHERE a.parent_note_id IS NOT NULL
              ),
              updated AS (
                  UPDATE notes SET deleted_at = NULL
                  WHERE id IN (SELECT id FROM ancestors WHERE id != @NoteId)
                    AND deleted_at IS NOT NULL
                  RETURNING id, title, is_folder
              )
              SELECT id AS Id, title AS Title, is_folder AS IsFolder FROM updated",
            new { NoteId = noteId });

        return rows.Select(r => new RestoredAncestor { NoteId = r.Id, Title = r.Title, IsFolder = r.IsFolder })
                   .ToList();
    }

    public virtual async Task InsertNoteLogAsync(
        IDbContext db, string noteId, string userId, string projectId, string title, bool isFolder)
    {
        var logMessage = isFolder ? "Note folder restored" : "Note restored";
        var newData    = JsonSerializer.Serialize(new { id = noteId, project_id = projectId, title });
        await db.ExecuteAsync(
            "INSERT INTO audit.note_logs (note_id, changed_by, operation, log_message, new_data) VALUES (@NoteId, @ChangedBy, @Operation, @LogMessage, @NewData::jsonb)",
            new { NoteId = noteId, ChangedBy = userId, Operation = "UPDATE", LogMessage = logMessage, NewData = newData });
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string noteId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'NOTE', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = noteId, LogMessage = logMessage, UserId = userId });
    }
}
