using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.ProjectTasks.GetProjectTasks;

[Route("projects/{projectId}/tasks")]
public class GetProjectTasksController(
    IDbContextFactory dbContextFactory,
    GetProjectTasksRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetProjectTasks(
        string projectId,
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var tasks = filter?.ToLowerInvariant() switch
        {
            "today"    => await repo.GetTasksDueTodayAsync(db, securityResult.FirebaseUid, projectId),
            "upcoming" => await repo.GetTasksDueUpcomingAsync(db, securityResult.FirebaseUid, projectId),
            _          => await repo.GetAllOpenTasksAsync(db, securityResult.FirebaseUid, projectId),
        };

        return Ok(tasks.ToResponse());
    }
}
