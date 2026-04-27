using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.ProjectTasks.CompleteProjectTask;

[Route("projects/{projectId}/tasks/{taskId}/complete")]
public class CompleteProjectTaskController(
    IDbContextFactory dbContextFactory,
    CompleteProjectTaskRepository repo,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpPatch]
    public async Task<IActionResult> CompleteProjectTask(
        string projectId,
        string taskId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var found = await repo.CompleteTaskAsync(db, taskId, projectId);
        if (!found)
            return NotFound(new { error = $"Task '{taskId}' was not found in project '{projectId}'." });

        return Ok();
    }
}
