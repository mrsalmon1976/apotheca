namespace Apotheca.Api.Features.Notes.SaveNoteFolder;

public class SaveNoteFolderRequest
{
    public string Title { get; init; } = string.Empty;
    public string? ParentNoteId { get; init; }
}
