using Apotheca.Data;

namespace Apotheca.Api.Features.Projects.SaveProject;

public class SaveProjectRepository
{
    public virtual async Task<bool> SaveProjectAsync(
        IDbContext db, string projectId, string name, string? summary)
    {
        var rows = await db.ExecuteAsync(
            @"UPDATE projects
              SET name    = @Name,
                  summary = @Summary
              WHERE id = @Id",
            new { Id = projectId, Name = name, Summary = summary });
        return rows > 0;
    }
}
