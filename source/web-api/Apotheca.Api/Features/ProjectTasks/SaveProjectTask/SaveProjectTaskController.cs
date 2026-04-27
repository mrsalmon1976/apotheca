using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.ProjectTasks.SaveProjectTask;

[Route("projects/{projectId}/tasks")]
public class SaveProjectTaskController(
    IDbContextFactory dbContextFactory,
    SaveProjectTaskRepository repo,
    SaveProjectTaskValidator validator,
    ISecurityProvider securityProvider) : AuthenticatedBaseController
{
    [HttpPost]
    public async Task<IActionResult> SaveProjectTask(
        string projectId,
        [FromBody] SaveProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = validator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var isNew = string.IsNullOrEmpty(request.Id);

        if (isNew)
        {
            var newId = await repo.InsertTaskAsync(db, projectId, securityResult.UserId, request);
            return CreatedAtAction(nameof(SaveProjectTask), new { projectId }, new { id = newId });
        }
        else
        {
            await repo.UpdateTaskAsync(db, request.Id!, projectId, request);
            return Ok();
        }
    }
}
