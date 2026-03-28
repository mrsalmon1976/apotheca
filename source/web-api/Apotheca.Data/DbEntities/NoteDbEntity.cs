namespace Apotheca.Data.DbEntities;

public class NoteDbEntity
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
}
