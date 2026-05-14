using Apotheca.Data;

namespace Apotheca.Api.Events.Notes.NoteDeleted;

public record DeletedDescendant(string Id, string Title, bool IsFolder);

public class NoteDeletedRepository
{
    public virtual async Task<IReadOnlyList<DeletedDescendant>> SoftDeleteDescendantsAsync(
        IDbContext db, string noteId)
    {
        var rows = await db.QueryAsync<DeletedDescendant>(
            @"WITH RECURSIVE descendants AS (
                  SELECT id, is_folder FROM notes
                  WHERE parent_note_id = @NoteId AND deleted_at IS NULL
                  UNION ALL
                  SELECT n.id, n.is_folder FROM notes n
                  INNER JOIN descendants d ON n.parent_note_id = d.id
                  WHERE n.deleted_at IS NULL
              ),
              updated AS (
                  UPDATE notes SET deleted_at = now() AT TIME ZONE 'UTC'
                  WHERE id IN (SELECT id FROM descendants)
                  RETURNING id, title, is_folder
              )
              SELECT id AS Id, title AS Title, is_folder AS IsFolder FROM updated",
            new { NoteId = noteId });

        return rows.ToList();
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string noteId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'NOTE', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = noteId, LogMessage = logMessage, UserId = userId });
    }
}
