namespace Apotheca.Api.Features.Documents.SaveDocument;

public class SaveDocumentRequest
{
    public string? Title { get; init; }
    public IReadOnlyList<string>? Labels { get; init; }
}
