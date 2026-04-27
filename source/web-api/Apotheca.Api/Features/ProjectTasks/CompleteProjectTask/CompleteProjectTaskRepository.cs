using Apotheca.Data;

namespace Apotheca.Api.Features.ProjectTasks.CompleteProjectTask;

public class CompleteProjectTaskRepository
{
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
