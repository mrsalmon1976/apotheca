using Apotheca.Data;

namespace Apotheca.Api.Features.Workspaces.SaveWorkspace;

public class SaveWorkspaceRepository
{
    public virtual async Task<bool> SaveWorkspaceAsync(IDbContext db, string workspaceId, string name)
    {
        var rows = await db.ExecuteAsync(
            @"UPDATE workspaces
              SET name       = @Name,
                  updated_at = now()
              WHERE id = @Id",
            new { Id = workspaceId, Name = name });
        return rows > 0;
    }
}
