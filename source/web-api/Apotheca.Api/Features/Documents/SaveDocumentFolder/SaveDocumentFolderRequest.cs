namespace Apotheca.Api.Features.Documents.SaveDocumentFolder;

public class SaveDocumentFolderRequest
{
    public string Title { get; init; } = string.Empty;
    public string? ParentDocumentId { get; init; }
    public IReadOnlyList<string> Labels { get; init; } = [];
}
