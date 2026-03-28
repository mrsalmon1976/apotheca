namespace Apotheca.Api.Features.Notes.GetNote;

public class GetNoteResponse
{
    public string Id { get; init; } = string.Empty;
    public string? ParentNoteId { get; init; }
    public bool IsFolder { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Body { get; init; }
    public IReadOnlyList<string> Labels { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
