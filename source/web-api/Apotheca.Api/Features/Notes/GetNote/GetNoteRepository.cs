using Apotheca.Data;

namespace Apotheca.Api.Features.Notes.GetNote;

public class GetNoteRepository
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
        public DateTimeOffset? DeletedAt { get; init; }
        public string? LabelsCsv { get; init; }
    }

    public virtual async Task<GetNoteResponse?> GetNoteAsync(
        IDbContext db, string projectId, string noteId)
    {
        var row = await db.QueryFirstOrDefaultAsync<NoteRow>(
            @"SELECT n.id              AS Id,
                     n.project_id     AS ProjectId,
                     n.parent_note_id AS ParentNoteId,
                     n.is_folder      AS IsFolder,
                     n.title          AS Title,
                     n.body           AS Body,
                     n.created_by     AS CreatedBy,
                     n.created_at     AS CreatedAt,
                     n.updated_at     AS UpdatedAt,
                     n.deleted_at     AS DeletedAt,
                     string_agg(l.label_text, ',' ORDER BY l.label_text) AS LabelsCsv
              FROM notes n
              LEFT JOIN note_labels nl ON nl.note_id = n.id
              LEFT JOIN labels l       ON l.id = nl.label_id
              WHERE n.id = @NoteId
                AND n.project_id = @ProjectId
              GROUP BY n.id",
            new { NoteId = noteId, ProjectId = projectId });

        if (row is null) return null;

        return new GetNoteResponse
        {
            Id           = row.Id,
            ParentNoteId = row.ParentNoteId,
            IsFolder     = row.IsFolder,
            Title        = row.Title,
            Body         = row.Body,
            Labels       = row.LabelsCsv?.Split(',') ?? [],
            CreatedAt    = row.CreatedAt,
            UpdatedAt    = row.UpdatedAt,
            DeletedAt    = row.DeletedAt,
        };
    }
}
