using Apotheca.Data;
using Apotheca.Data.DbEntities;

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
                 t.created_by     AS CreatedBy,
                 t.priority       AS Priority,
                 t.due_at         AS DueAt,
                 t.created_at     AS CreatedAt,
                 t.updated_at     AS UpdatedAt,
                 t.completed_at   AS CompletedAt
          FROM tasks t
          INNER JOIN user_projects up ON up.project_id = t.project_id
          INNER JOIN user_firebase_identities ufi ON ufi.user_id = up.user_id
          WHERE ufi.firebase_uid = @FirebaseUid
            AND t.project_id = @ProjectId
            AND t.completed_at IS NULL";

    public virtual async Task<IEnumerable<TaskDbEntity>> GetAllOpenTasksAsync(
        IDbContext db, string firebaseUid, string projectId)
    {
        return await db.QueryAsync<TaskDbEntity>(
            BaseQuery + " ORDER BY t.due_at, t.created_at",
            new { FirebaseUid = firebaseUid, ProjectId = projectId });
    }

    public virtual async Task<IEnumerable<TaskDbEntity>> GetTasksDueTodayAsync(
        IDbContext db, string firebaseUid, string projectId)
    {
        return await db.QueryAsync<TaskDbEntity>(
            BaseQuery + " AND t.due_at::date <= CURRENT_DATE ORDER BY t.due_at, t.created_at",
            new { FirebaseUid = firebaseUid, ProjectId = projectId });
    }

    public virtual async Task<IEnumerable<TaskDbEntity>> GetTasksDueUpcomingAsync(
        IDbContext db, string firebaseUid, string projectId)
    {
        return await db.QueryAsync<TaskDbEntity>(
            BaseQuery + " AND t.due_at > now() AND t.due_at <= now() + INTERVAL '7 days' ORDER BY t.due_at, t.created_at",
            new { FirebaseUid = firebaseUid, ProjectId = projectId });
    }
}
