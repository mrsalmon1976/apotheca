namespace Apotheca.Api.Features.Notes.GetNotes;

public class GetNotesResponse
{
    public string Id { get; init; } = string.Empty;
    public string? ParentNoteId { get; init; }
    public bool IsFolder { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
