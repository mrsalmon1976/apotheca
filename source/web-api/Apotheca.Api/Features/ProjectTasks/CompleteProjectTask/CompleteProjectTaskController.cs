using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.ProjectTasks.CompleteProjectTask;

[Route("projects/{projectId}/tasks/{taskId}/complete")]
public class CompleteProjectTaskController(
    IDbContextFactory dbContextFactory,
    CompleteProjectTaskRepository repo) : AuthenticatedBaseController
{
    [HttpPatch]
    public async Task<IActionResult> CompleteProjectTask(
        string projectId,
        string taskId,
        CancellationToken cancellationToken)
    {
        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var hasAccess = await repo.UserHasProjectAccessAsync(db, firebaseUid, projectId);
        if (!hasAccess)
            return Forbid();

        var found = await repo.CompleteTaskAsync(db, taskId, projectId);
        if (!found)
            return NotFound(new { error = $"Task '{taskId}' was not found in project '{projectId}'." });

        return Ok();
    }
}
