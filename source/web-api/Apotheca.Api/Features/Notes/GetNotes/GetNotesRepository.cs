using Apotheca.Data;

namespace Apotheca.Api.Features.Notes.GetNotes;

public class GetNotesRepository
{
    private class NoteRow
    {
        public string Id { get; init; } = string.Empty;
        public string ProjectId { get; init; } = string.Empty;
        public string? ParentNoteId { get; init; }
        public bool IsFolder { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Body { get; init; }
        public string CreatedBy { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public string? LabelsCsv { get; init; }
    }

    public virtual async Task<IEnumerable<GetNotesResponse>> GetNotesAsync(
        IDbContext db, string projectId, string? parentNoteId)
    {
        IEnumerable<NoteRow> rows;

        if (parentNoteId is null)
        {
            rows = await db.QueryAsync<NoteRow>(
                @"SELECT n.id              AS Id,
                         n.project_id     AS ProjectId,
                         n.parent_note_id AS ParentNoteId,
                         n.is_folder      AS IsFolder,
                         n.title          AS Title,
                         n.body           AS Body,
                         n.created_by     AS CreatedBy,
                         n.created_at     AS CreatedAt,
                         n.updated_at     AS UpdatedAt,
                         string_agg(l.label_text, ',' ORDER BY l.label_text) AS LabelsCsv
                  FROM notes n
                  LEFT JOIN note_labels nl ON nl.note_id = n.id
                  LEFT JOIN labels l       ON l.id = nl.label_id
                  WHERE n.project_id = @ProjectId
                    AND n.parent_note_id IS NULL
                    AND n.deleted_at IS NULL
                  GROUP BY n.id
                  ORDER BY n.is_folder DESC, lower(n.title)",
                new { ProjectId = projectId });
        }
        else
        {
            rows = await db.QueryAsync<NoteRow>(
                @"SELECT n.id              AS Id,
                         n.project_id     AS ProjectId,
                         n.parent_note_id AS ParentNoteId,
                         n.is_folder      AS IsFolder,
                         n.title          AS Title,
                         n.body           AS Body,
                         n.created_by     AS CreatedBy,
                         n.created_at     AS CreatedAt,
                         n.updated_at     AS UpdatedAt,
                         string_agg(l.label_text, ',' ORDER BY l.label_text) AS LabelsCsv
                  FROM notes n
                  LEFT JOIN note_labels nl ON nl.note_id = n.id
                  LEFT JOIN labels l       ON l.id = nl.label_id
                  WHERE n.project_id = @ProjectId
                    AND n.parent_note_id = @ParentNoteId
                    AND n.deleted_at IS NULL
                  GROUP BY n.id
                  ORDER BY n.is_folder DESC, lower(n.title)",
                new { ProjectId = projectId, ParentNoteId = parentNoteId });
        }

        return rows.Select(r => new GetNotesResponse
        {
            Id           = r.Id,
            ParentNoteId = r.ParentNoteId,
            IsFolder     = r.IsFolder,
            Title        = r.Title,
            Body         = r.Body,
            Labels       = r.LabelsCsv?.Split(',') ?? [],
            CreatedAt    = r.CreatedAt,
            UpdatedAt    = r.UpdatedAt,
        });
    }
}
