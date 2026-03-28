using Apotheca.Data;

namespace Apotheca.Api.Features.Labels.SearchLabels;

public class SearchLabelsRepository
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

    public virtual async Task<IEnumerable<SearchLabelsResponse>> SearchAsync(
        IDbContext db, string projectId, string query)
    {
        return await db.QueryAsync<SearchLabelsResponse>(
            @"SELECT id        AS Id,
                     label_text AS LabelText
              FROM labels
              WHERE project_id = @ProjectId
                AND label_text LIKE @Query
              ORDER BY label_text
              LIMIT 3",
            new { ProjectId = projectId, Query = query + "%" });
    }
}
