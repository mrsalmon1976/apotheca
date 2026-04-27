using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.SaveNoteFolder;

[Route("projects/{projectId}/notes/folders")]
public class SaveNoteFolderController(
    IDbContextFactory dbContextFactory,
    SaveNoteFolderRepository repo,
    SaveNoteFolderValidator validator,
    ISecurityProvider securityProvider,
    ILogger<SaveNoteFolderController> logger) : AuthenticatedBaseController
{
    [HttpPost]
    public async Task<IActionResult> SaveNoteFolder(
        string projectId,
        [FromBody] SaveNoteFolderRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = validator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        await db.BeginTransactionAsync(cancellationToken);

        var id = await repo.InsertNoteFolderAsync(db, projectId, securityResult.UserId, request.Title.Trim(), request.ParentNoteId);

        var labelTexts = request.Labels
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var labelText in labelTexts)
        {
            var labelId = await repo.UpsertLabelAsync(db, projectId, securityResult.UserId, labelText);
            await repo.InsertNoteLabelAsync(db, id, labelId);
        }

        await repo.InsertNoteLogAsync(db, id, securityResult.UserId, projectId);
        await repo.InsertProjectActivityLogAsync(db, projectId, id, securityResult.UserId, $"Note folder '{request.Title.Trim()}' added");

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Note folder created. FolderId: {FolderId}, ProjectId: {ProjectId}, UserId: {UserId}", id, projectId, securityResult.UserId);

        return CreatedAtAction(nameof(SaveNoteFolder), new { projectId }, new { id });
    }
}
