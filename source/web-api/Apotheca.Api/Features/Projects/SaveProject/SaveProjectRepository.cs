using Apotheca.Data;

namespace Apotheca.Api.Features.Projects.SaveProject;

public class SaveProjectRepository
{
    public virtual async Task<bool> UserHasProjectAccessAsync(
        IDbContext db, string firebaseUid, string projectId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            @"SELECT COUNT(1)
              FROM user_projects up
              INNER JOIN user_firebase_identities ufi ON ufi.user_id = up.user_id
              WHERE ufi.firebase_uid = @FirebaseUid
                AND up.project_id = @ProjectId",
            new { FirebaseUid = firebaseUid, ProjectId = projectId });
        return count > 0;
    }

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
