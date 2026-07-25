using Apotheca.Data;

namespace Apotheca.Api.Features.Notes.MoveNote;

public record MoveNoteInfo(string Title, bool IsFolder, string? ParentNoteId);

public class MoveNoteRepository
{
    public virtual async Task<MoveNoteInfo?> GetNoteInfoAsync(
        IDbContext db, string projectId, string noteId)
    {
        return await db.QueryFirstOrDefaultAsync<MoveNoteInfo?>(
            @"SELECT title AS Title, is_folder AS IsFolder, parent_note_id AS ParentNoteId
              FROM notes
              WHERE id = @NoteId
                AND project_id = @ProjectId
                AND deleted_at IS NULL",
            new { NoteId = noteId, ProjectId = projectId });
    }

    public virtual async Task<bool> TargetFolderExistsAsync(
        IDbContext db, string projectId, string folderId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM notes WHERE id = @FolderId AND project_id = @ProjectId AND is_folder = TRUE AND deleted_at IS NULL",
            new { FolderId = folderId, ProjectId = projectId });
        return count > 0;
    }

    public virtual async Task<string?> GetFolderTitleAsync(IDbContext db, string folderId)
    {
        return await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT title FROM notes WHERE id = @FolderId",
            new { FolderId = folderId });
    }

    public virtual async Task<bool> WouldCreateCycleAsync(
        IDbContext db, string noteId, string targetFolderId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            @"WITH RECURSIVE ancestors AS (
                  SELECT id, parent_note_id FROM notes WHERE id = @TargetFolderId
                  UNION ALL
                  SELECT n.id, n.parent_note_id
                  FROM notes n
                  JOIN ancestors a ON n.id = a.parent_note_id
              )
              SELECT COUNT(1) FROM ancestors WHERE id = @NoteId",
            new { NoteId = noteId, TargetFolderId = targetFolderId });
        return count > 0;
    }

    public virtual async Task MoveNoteAsync(
        IDbContext db, string projectId, string noteId, string? targetFolderId)
    {
        await db.ExecuteAsync(
            @"UPDATE notes SET parent_note_id = @TargetFolderId, updated_at = now()
              WHERE id = @NoteId AND project_id = @ProjectId",
            new { NoteId = noteId, ProjectId = projectId, TargetFolderId = targetFolderId });
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string noteId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'NOTE', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = noteId, LogMessage = logMessage, UserId = userId });
    }
}
