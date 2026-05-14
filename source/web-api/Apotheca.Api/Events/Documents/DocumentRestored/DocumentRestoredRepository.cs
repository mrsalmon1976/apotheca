using Apotheca.Data;

namespace Apotheca.Api.Events.Documents.DocumentRestored;

public class DocumentRestoredRepository
{
    public virtual async Task InsertProjectActivityLogAsync(
        IDbContext db, string projectId, string documentId, string userId, string logMessage)
    {
        await db.ExecuteAsync(
            "INSERT INTO audit.project_activity_logs (project_id, ref_id, ref_type, log_message, user_id) VALUES (@ProjectId, @RefId, 'DOCUMENT', @LogMessage, @UserId)",
            new { ProjectId = projectId, RefId = documentId, LogMessage = logMessage, UserId = userId });
    }
}
