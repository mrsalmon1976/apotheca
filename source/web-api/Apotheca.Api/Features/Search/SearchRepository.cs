using Apotheca.Data;

namespace Apotheca.Api.Features.Search;

public class SearchRepository
{
    public virtual async Task<IEnumerable<SearchResult>> SearchAsync(
        IDbContext db,
        string userId,
        string query,
        string[] types,
        bool searchTitle,
        bool searchBody)
    {
        var (vectorCondition, rankExpression) = (searchTitle, searchBody) switch
        {
            (true, false) => (
                "s.title_vector @@ plainto_tsquery('english', @Query)",
                "ts_rank(s.title_vector, plainto_tsquery('english', @Query))"),
            (false, true) => (
                "s.body_vector @@ plainto_tsquery('english', @Query)",
                "ts_rank(s.body_vector, plainto_tsquery('english', @Query))"),
            _ => (
                "s.search_vector @@ plainto_tsquery('english', @Query)",
                "ts_rank(s.search_vector, plainto_tsquery('english', @Query))"),
        };

        var headlineSource = searchBody
            ? "COALESCE(NULLIF(s.text_body, ''), s.text_title)"
            : "s.text_title";

        var sql = $"""
            SELECT
                s.reference_id   AS ReferenceId,
                s.reference_type AS ReferenceType,
                s.text_title     AS Title,
                ts_headline('english', {headlineSource}, plainto_tsquery('english', @Query),
                    'MaxWords=35,MinWords=15,MaxFragments=2,StartSel=<b>,StopSel=</b>') AS Snippet,
                s.project_id     AS ProjectId
            FROM search s
            INNER JOIN project_users up
                ON s.project_id = up.project_id
               AND up.user_id = @UserId
            WHERE {vectorCondition}
              AND LOWER(s.reference_type) = ANY(@Types)
            ORDER BY {rankExpression} DESC
            LIMIT 50
            """;

        return await db.QueryAsync<SearchResult>(sql, new { Query = query, Types = types, UserId = userId });
    }
}
