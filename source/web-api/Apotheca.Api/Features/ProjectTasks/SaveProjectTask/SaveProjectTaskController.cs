using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.ProjectTasks.SaveProjectTask;

[Route("projects/{projectId}/tasks")]
public class SaveProjectTaskController(
    IDbContextFactory dbContextFactory,
    SaveProjectTaskRepository repo,
    SaveProjectTaskValidator validator) : AuthenticatedBaseController
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

        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var hasAccess = await repo.UserHasProjectAccessAsync(db, firebaseUid, projectId);
        if (!hasAccess)
            return Forbid();

        var isNew = string.IsNullOrEmpty(request.Id);

        if (isNew)
        {
            var userId = await repo.GetUserIdAsync(db, firebaseUid);
            if (userId is null)
                return Unauthorized(new { error = "User identity could not be determined." });

            var newId = await repo.InsertTaskAsync(db, projectId, userId, request);
            return CreatedAtAction(nameof(SaveProjectTask), new { projectId }, new { id = newId });
        }
        else
        {
            await repo.UpdateTaskAsync(db, request.Id!, projectId, request);
            return Ok();
        }
    }
}
