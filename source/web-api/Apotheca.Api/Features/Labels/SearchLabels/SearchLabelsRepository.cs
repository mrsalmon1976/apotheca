using Apotheca.Data;

namespace Apotheca.Api.Features.Labels.SearchLabels;

public class SearchLabelsRepository
{
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
