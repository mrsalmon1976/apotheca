namespace Apotheca.Api.Features.Notes.GetProjectNotes;

public class GetProjectNotesResponse
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Body { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public string? CreatedByDisplayName { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
