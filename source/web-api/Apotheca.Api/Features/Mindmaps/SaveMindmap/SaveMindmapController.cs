using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Mindmaps.SaveMindmap;

[Route("projects/{projectId}/mindmaps/{mindmapId}")]
public class SaveMindmapController(
    IDbContextFactory dbContextFactory,
    SaveMindmapRepository repo,
    SaveMindmapValidator validator,
    ISecurityProvider securityProvider,
    ILogger<SaveMindmapController> logger) : AuthenticatedBaseController
{
    [HttpPatch]
    public async Task<IActionResult> SaveMindmap(
        string projectId,
        string mindmapId,
        [FromBody] SaveMindmapRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = validator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var exists = await repo.MindmapExistsAsync(db, projectId, mindmapId);
        if (!exists)
            return NotFound(new { error = $"Mindmap '{mindmapId}' was not found." });

        var name = request.Name.Trim();

        await db.BeginTransactionAsync(cancellationToken);

        await repo.UpdateMindmapNameAsync(db, projectId, mindmapId, name);
        await repo.UpdateSearchTitleAsync(db, mindmapId, name);

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Mindmap saved. MindmapId: {MindmapId}, ProjectId: {ProjectId}, UserId: {UserId}", mindmapId, projectId, securityResult.UserId);

        return Ok();
    }
}
