using Apotheca.Data;

namespace Apotheca.Api.Features.Notes.GetProjectNotes;

public class GetProjectNotesRepository
{
    private const string BaseQuery =
        @"SELECT n.id              AS Id,
                 n.title           AS Title,
                 n.body            AS Body,
                 n.created_by      AS CreatedBy,
                 u.display_name    AS CreatedByDisplayName,
                 n.updated_at      AS UpdatedAt
          FROM notes n
          LEFT JOIN users u ON u.id = n.created_by
          WHERE n.project_id = @ProjectId
            AND n.is_folder = false
            AND n.deleted_at IS NULL
          ORDER BY n.updated_at DESC";

    private static string WithLimit(string sql, int? limit) =>
        limit is > 0 ? sql + " LIMIT @Limit" : sql;

    public virtual async Task<IEnumerable<ProjectNoteModel>> GetRecentNotesAsync(
        IDbContext db, string projectId, int? limit = null)
    {
        var sql = WithLimit(BaseQuery, limit);
        return await db.QueryAsync<ProjectNoteModel>(sql, new { ProjectId = projectId, Limit = limit });
    }
}
