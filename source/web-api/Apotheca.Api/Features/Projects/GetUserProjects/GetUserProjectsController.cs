using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Projects.GetUserProjects;

[Route("projects")]
public class GetUserProjectsController(
    IDbContextFactory dbContextFactory,
    GetUserProjectsRepository repo) : AuthenticatedBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetUserProjects(CancellationToken cancellationToken)
    {
        var firebaseUid = GetFirebaseUid();
        if (firebaseUid is null)
            return Unauthorized(new { error = "User identity could not be determined." });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var projects = await repo.GetProjectsByUidAsync(db, firebaseUid);
        return Ok(projects.ToResponse());
    }
}
