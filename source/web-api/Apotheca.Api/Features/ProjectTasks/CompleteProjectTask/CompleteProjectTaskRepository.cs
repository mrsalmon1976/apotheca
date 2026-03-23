using Apotheca.Data;

namespace Apotheca.Api.Features.ProjectTasks.CompleteProjectTask;

public class CompleteProjectTaskRepository
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

    public virtual async Task<bool> CompleteTaskAsync(
        IDbContext db, string taskId, string projectId)
    {
        var rows = await db.ExecuteAsync(
            @"UPDATE tasks
              SET completed_at = now(),
                  updated_at   = now()
              WHERE id         = @Id
                AND project_id = @ProjectId",
            new { Id = taskId, ProjectId = projectId });
        return rows > 0;
    }
}
