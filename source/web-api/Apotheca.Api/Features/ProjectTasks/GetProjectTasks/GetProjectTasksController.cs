using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.ProjectTasks.GetProjectTasks;

[Route("projects/{projectId}/tasks")]
public class GetProjectTasksController(
    IDbContextFactory dbContextFactory,
    GetProjectTasksRepository repo) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetProjectTasks(
        string projectId,
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
    {
        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var hasAccess = await repo.UserHasProjectAccessAsync(db, firebaseUid, projectId);
        if (!hasAccess)
            return Forbid();

        var tasks = filter?.ToLowerInvariant() switch
        {
            "today"    => await repo.GetTasksDueTodayAsync(db, firebaseUid, projectId),
            "upcoming" => await repo.GetTasksDueUpcomingAsync(db, firebaseUid, projectId),
            _          => await repo.GetAllOpenTasksAsync(db, firebaseUid, projectId),
        };

        return Ok(tasks.ToResponse());
    }
}
