using Apotheca.Data;

namespace Apotheca.Api.Features.ProjectTasks.GetProjectTasks;

public class GetProjectTasksRepository
{
    private const string BaseQuery =
        @"SELECT t.id              AS Id,
                 t.project_id     AS ProjectId,
                 t.parent_task_id AS ParentTaskId,
                 t.title          AS Title,
                 t.notes          AS Notes,
                 t.assigned_to    AS AssignedTo,
                 u.display_name   AS AssignedToDisplayName,
                 t.created_by     AS CreatedBy,
                 t.priority       AS Priority,
                 t.due_at         AS DueAt,
                 t.created_at     AS CreatedAt,
                 t.updated_at     AS UpdatedAt,
                 t.completed_at   AS CompletedAt
          FROM tasks t
          INNER JOIN project_users up ON up.project_id = t.project_id
          INNER JOIN user_firebase_identities ufi ON ufi.user_id = up.user_id
          LEFT JOIN users u ON u.id = t.assigned_to
          WHERE ufi.firebase_uid = @FirebaseUid
            AND t.project_id = @ProjectId
            AND t.completed_at IS NULL";

    private static string WithLimit(string sql, int? limit) =>
        limit is > 0 ? sql + " LIMIT @Limit" : sql;

    public virtual async Task<IEnumerable<ProjectTaskModel>> GetAllOpenTasksAsync(
        IDbContext db, string firebaseUid, string projectId, int? limit = null)
    {
        var sql = WithLimit(BaseQuery + " ORDER BY t.due_at NULLS LAST, t.created_at", limit);
        return await db.QueryAsync<ProjectTaskModel>(sql, new { FirebaseUid = firebaseUid, ProjectId = projectId, Limit = limit });
    }

    public virtual async Task<IEnumerable<ProjectTaskModel>> GetTasksDueTodayAsync(
        IDbContext db, string firebaseUid, string projectId, int? limit = null)
    {
        var sql = WithLimit(BaseQuery + " AND t.due_at::date <= CURRENT_DATE ORDER BY t.due_at, t.created_at", limit);
        return await db.QueryAsync<ProjectTaskModel>(sql, new { FirebaseUid = firebaseUid, ProjectId = projectId, Limit = limit });
    }

    public virtual async Task<IEnumerable<ProjectTaskModel>> GetTasksDueUpcomingAsync(
        IDbContext db, string firebaseUid, string projectId, int? limit = null)
    {
        var sql = WithLimit(BaseQuery + " AND t.due_at > now() AND t.due_at <= now() + INTERVAL '7 days' ORDER BY t.due_at, t.created_at", limit);
        return await db.QueryAsync<ProjectTaskModel>(sql, new { FirebaseUid = firebaseUid, ProjectId = projectId, Limit = limit });
    }
}
