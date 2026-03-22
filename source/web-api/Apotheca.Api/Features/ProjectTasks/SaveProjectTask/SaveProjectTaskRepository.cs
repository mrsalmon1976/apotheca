using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.ProjectTasks.SaveProjectTask;

public class SaveProjectTaskRepository
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

    public virtual async Task<string?> GetUserIdAsync(IDbContext db, string firebaseUid)
    {
        return await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT user_id FROM user_firebase_identities WHERE firebase_uid = @FirebaseUid",
            new { FirebaseUid = firebaseUid });
    }

    public virtual async Task<string> InsertTaskAsync(
        IDbContext db, string projectId, string userId, SaveProjectTaskRequest request)
    {
        var id = Nanoid.Generate();
        await db.ExecuteAsync(
            @"INSERT INTO tasks (id, project_id, parent_task_id, title, notes, assigned_to, created_by, priority, due_at)
              VALUES (@Id, @ProjectId, @ParentTaskId, @Title, @Notes, @AssignedTo, @CreatedBy, @Priority, @DueAt)",
            new
            {
                Id           = id,
                ProjectId    = projectId,
                ParentTaskId = request.ParentTaskId,
                Title        = request.Title,
                Notes        = request.Notes,
                AssignedTo   = request.AssignedTo,
                CreatedBy    = userId,
                Priority     = request.Priority,
                DueAt        = request.DueAt,
            });
        return id;
    }

    public virtual async Task UpdateTaskAsync(
        IDbContext db, string taskId, string projectId, SaveProjectTaskRequest request)
    {
        var rows = await db.ExecuteAsync(
            @"UPDATE tasks
              SET parent_task_id = @ParentTaskId,
                  title          = @Title,
                  notes          = @Notes,
                  assigned_to    = @AssignedTo,
                  priority       = @Priority,
                  due_at         = @DueAt,
                  updated_at     = now()
              WHERE id = @Id
                AND project_id = @ProjectId",
            new
            {
                Id           = taskId,
                ProjectId    = projectId,
                ParentTaskId = request.ParentTaskId,
                Title        = request.Title,
                Notes        = request.Notes,
                AssignedTo   = request.AssignedTo,
                Priority     = request.Priority,
                DueAt        = request.DueAt,
            });

        if (rows == 0)
            throw new InvalidOperationException($"Task '{taskId}' was not found in project '{projectId}'.");
    }
}
