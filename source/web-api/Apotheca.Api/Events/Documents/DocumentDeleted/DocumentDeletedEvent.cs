namespace Apotheca.Api.Events.Documents.DocumentDeleted;

public class DocumentDeletedEvent
{
    public const string TopicId = "document-deleted";

    public string DocumentId { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public bool IsFolder { get; init; }
    public DateTimeOffset DeletedAt { get; init; }
}
