using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.ProjectTasks.SaveProjectTask;

public class SaveProjectTaskRepository
{
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

    public virtual async Task UpsertSearchAsync(IDbContext db, string projectId, string taskId, string title, string body)
    {
        await db.ExecuteAsync(
            @"INSERT INTO search (reference_id, reference_type, project_id, text_title, text_body, updated_at)
              VALUES (@ReferenceId, 'task', @ProjectId, @Title, @Body, now())
              ON CONFLICT (reference_id, reference_type) DO UPDATE
              SET project_id = EXCLUDED.project_id,
                  text_title = EXCLUDED.text_title,
                  text_body  = EXCLUDED.text_body,
                  updated_at = now()",
            new { ReferenceId = taskId, ProjectId = projectId, Title = title, Body = body });
    }
}
