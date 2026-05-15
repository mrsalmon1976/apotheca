using Apotheca.Data;

namespace Apotheca.Api.Features.Notes.RenameNoteFolder;

public class RenameNoteFolderRepository
{
    public virtual async Task<bool> FolderExistsAsync(IDbContext db, string projectId, string folderId)
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

    public virtual async Task RenameFolderAsync(IDbContext db, string projectId, string folderId, string title)
    {
        await db.ExecuteAsync(
            @"UPDATE notes SET title = @Title, updated_at = now()
              WHERE id = @FolderId AND project_id = @ProjectId",
            new { Title = title, FolderId = folderId, ProjectId = projectId });
    }

    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string folderId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'NOTE', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = folderId, LogMessage = logMessage, UserId = userId });
    }
}
