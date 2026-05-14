using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.UserTasks.GetUserTasks;

[Route("tasks")]
public class GetUserTasksController(
    IDbContextFactory dbContextFactory,
    GetUserTasksRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetUserTasks(
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeAccessAsync(db, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var tasks = filter?.ToLowerInvariant() switch
        {
            "today"            => await repo.GetTasksDueTodayAsync(db, securityResult.FirebaseUid),
            "upcoming"         => await repo.GetTasksDueUpcomingAsync(db, securityResult.FirebaseUid),
            "overdue-upcoming" => await repo.GetOverdueAndUpcomingTasksAsync(db, securityResult.FirebaseUid),
            _                  => await repo.GetAllOpenTasksAsync(db, securityResult.FirebaseUid),
        };

        return Ok(tasks);
    }
}
