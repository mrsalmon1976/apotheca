namespace Apotheca.Api.Features.Documents.GetDocument;

public class GetDocumentResponse
{
    public string Id { get; init; } = string.Empty;
    public string? ParentDocumentId { get; init; }
    public bool IsFolder { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? FileName { get; init; }
    public string? FileExtension { get; init; }
    public string? Mimetype { get; init; }
    public long? FileLength { get; init; }
    public IReadOnlyList<string> Labels { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? DeletedAt { get; init; }
}
