using Apotheca.Data;

namespace Apotheca.Api.Features.Projects.GetProjectOverview;

public class GetProjectOverviewRepository
{
    public virtual async Task<int> GetOpenTaskCountAsync(
        IDbContext db, string firebaseUid, string projectId)
    {
        return await db.QueryFirstOrDefaultAsync<int>(
            @"SELECT COUNT(1)
              FROM tasks t
              INNER JOIN user_firebase_identities ufi ON ufi.user_id = t.assigned_to
              WHERE ufi.firebase_uid = @FirebaseUid
                AND t.project_id = @ProjectId
                AND t.completed_at IS NULL",
            new { FirebaseUid = firebaseUid, ProjectId = projectId });
    }

    public virtual async Task<int> GetNoteCountAsync(
        IDbContext db, string projectId)
    {
        return await db.QueryFirstOrDefaultAsync<int>(
            @"SELECT COUNT(1)
              FROM notes
              WHERE project_id = @ProjectId
                AND is_folder = FALSE
                AND deleted_at IS NULL",
            new { ProjectId = projectId });
    }

    public virtual async Task<int> GetDocumentCountAsync(
        IDbContext db, string projectId)
    {
        return await db.QueryFirstOrDefaultAsync<int>(
            @"SELECT COUNT(1)
              FROM documents
              WHERE project_id = @ProjectId
                AND is_folder = FALSE
                AND deleted_at IS NULL",
            new { ProjectId = projectId });
    }
}
