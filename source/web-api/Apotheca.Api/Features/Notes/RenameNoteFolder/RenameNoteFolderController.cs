using Apotheca.Api.Providers;
using Apotheca.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Api.Features.Notes.RenameNoteFolder;

[Route("projects/{projectId}/notes/folders/{folderId}")]
public class RenameNoteFolderController(
    IDbContextFactory dbContextFactory,
    RenameNoteFolderRepository repo,
    RenameNoteFolderValidator validator,
    ISecurityProvider securityProvider,
    ILogger<RenameNoteFolderController> logger) : AuthenticatedBaseController
{
    [HttpPatch]
    public async Task<IActionResult> RenameNoteFolder(
        string projectId,
        string folderId,
        [FromBody] RenameNoteFolderRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = validator.Validate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        await using var db = await dbContextFactory.CreateAsync(cancellationToken);

        var securityResult = await securityProvider.AuthorizeProjectAccessAsync(db, projectId, cancellationToken);
        if (!securityResult.IsAuthorized)
            return Unauthorized(new { error = securityResult.ErrorMessage });

        var folderExists = await repo.FolderExistsAsync(db, projectId, folderId);
        if (!folderExists)
            return NotFound(new { error = $"Folder '{folderId}' was not found." });

        var oldTitle = await repo.GetFolderTitleAsync(db, folderId);
        var title = request.Title.Trim();

        await db.BeginTransactionAsync(cancellationToken);

        await repo.RenameFolderAsync(db, projectId, folderId, title);
        await repo.InsertProjectActivityLogAsync(db, projectId, folderId, securityResult.UserId, $"Note folder '{oldTitle}' renamed to '{title}'");

        await db.CommitAsync(cancellationToken);

        logger.LogInformation("Note folder renamed. FolderId: {FolderId}, ProjectId: {ProjectId}, UserId: {UserId}", folderId, projectId, securityResult.UserId);

        return Ok();
    }
}
