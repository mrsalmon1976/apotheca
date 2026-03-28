namespace Apotheca.Api.Features.Notes.SaveNote;

public class SaveNoteRequest
{
    public string? Title  { get; init; }
    public string? Body   { get; init; }
    public IReadOnlyList<string>? Labels { get; init; }
}
