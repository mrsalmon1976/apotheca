using Apotheca.Data;

namespace Apotheca.Api.Features.UserTasks.GetUserTasks;

public class GetUserTasksRepository
{
    private const string BaseQuery =
        @"SELECT t.id              AS Id,
                 t.project_id     AS ProjectId,
                 p.name           AS ProjectName,
                 t.parent_task_id AS ParentTaskId,
                 t.title          AS Title,
                 t.notes          AS Notes,
                 t.assigned_to    AS AssignedTo,
                 t.created_by     AS CreatedBy,
                 t.priority       AS Priority,
                 t.due_at         AS DueAt,
                 t.created_at     AS CreatedAt,
                 t.updated_at     AS UpdatedAt,
                 t.completed_at   AS CompletedAt
          FROM tasks t
          INNER JOIN projects p ON p.id = t.project_id
          INNER JOIN user_projects up ON up.project_id = t.project_id
          INNER JOIN user_firebase_identities ufi ON ufi.user_id = up.user_id
          WHERE ufi.firebase_uid = @FirebaseUid
            AND t.completed_at IS NULL";

    public virtual async Task<IEnumerable<GetUserTasksResponse>> GetAllOpenTasksAsync(
        IDbContext db, string firebaseUid)
    {
        return await db.QueryAsync<GetUserTasksResponse>(
            BaseQuery + " ORDER BY t.due_at, t.created_at",
            new { FirebaseUid = firebaseUid });
    }

    public virtual async Task<IEnumerable<GetUserTasksResponse>> GetTasksDueTodayAsync(
        IDbContext db, string firebaseUid)
    {
        return await db.QueryAsync<GetUserTasksResponse>(
            BaseQuery + " AND t.due_at::date <= CURRENT_DATE ORDER BY t.due_at, t.created_at",
            new { FirebaseUid = firebaseUid });
    }

    public virtual async Task<IEnumerable<GetUserTasksResponse>> GetTasksDueUpcomingAsync(
        IDbContext db, string firebaseUid)
    {
        return await db.QueryAsync<GetUserTasksResponse>(
            BaseQuery + " AND t.due_at > now() AND t.due_at <= now() + INTERVAL '7 days' ORDER BY t.due_at, t.created_at",
            new { FirebaseUid = firebaseUid });
    }

    public virtual async Task<IEnumerable<GetUserTasksResponse>> GetOverdueAndUpcomingTasksAsync(
        IDbContext db, string firebaseUid)
    {
        return await db.QueryAsync<GetUserTasksResponse>(
            BaseQuery + " AND t.due_at IS NOT NULL AND t.due_at <= now() + INTERVAL '7 days' ORDER BY t.due_at, t.created_at",
            new { FirebaseUid = firebaseUid });
    }
}
