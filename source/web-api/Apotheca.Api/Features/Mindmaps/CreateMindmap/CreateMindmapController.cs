using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Mindmaps.CreateMindmap;

[Route("projects/{projectId}/mindmaps")]
public class CreateMindmapController(
    IDbContextFactory dbContextFactory,
    CreateMindmapRepository repo,
    CreateMindmapValidator validator,
    ISecurityProvider securityProvider,
    ILogger<CreateMindmapController> logger) : AuthenticatedBaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateMindmap(
        string projectId,
        [FromBody] CreateMindmapRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = validator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var name = request.Name.Trim();

        await db.BeginTransactionAsync(cancellationToken);

        var mindmapId = await repo.InsertMindmapAsync(db, projectId, securityResult.UserId, name);
        var rootNodeId = await repo.InsertRootNodeAsync(db, mindmapId, securityResult.UserId);
        await repo.UpsertSearchAsync(db, projectId, mindmapId, name, "");

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Mindmap created. MindmapId: {MindmapId}, ProjectId: {ProjectId}, UserId: {UserId}", mindmapId, projectId, securityResult.UserId);

        return CreatedAtAction(nameof(CreateMindmap), new { projectId }, new { id = mindmapId, rootNodeId });
    }
}
