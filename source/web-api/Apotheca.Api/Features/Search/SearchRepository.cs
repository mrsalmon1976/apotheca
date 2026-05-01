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
                "to_tsvector('english', s.text_title) @@ plainto_tsquery('english', @Query)",
                "ts_rank(to_tsvector('english', s.text_title), plainto_tsquery('english', @Query))"),
            (false, true) => (
                "to_tsvector('english', s.text_body) @@ plainto_tsquery('english', @Query)",
                "ts_rank(to_tsvector('english', s.text_body), plainto_tsquery('english', @Query))"),
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
                COALESCE(n.project_id, t.project_id) AS ProjectId
            FROM search s
            LEFT JOIN notes n
                ON s.reference_id = n.id
               AND LOWER(s.reference_type) = 'note'
               AND n.deleted_at IS NULL
            LEFT JOIN tasks t
                ON s.reference_id = t.id
               AND LOWER(s.reference_type) = 'task'
            INNER JOIN user_projects up
                ON COALESCE(n.project_id, t.project_id) = up.project_id
               AND up.user_id = @UserId
            WHERE {vectorCondition}
              AND LOWER(s.reference_type) = ANY(@Types)
            ORDER BY {rankExpression} DESC
            LIMIT 50
            """;

        return await db.QueryAsync<SearchResult>(sql, new { Query = query, Types = types, UserId = userId });
    }
}
