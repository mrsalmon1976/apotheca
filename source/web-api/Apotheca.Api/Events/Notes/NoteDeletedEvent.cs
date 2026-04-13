namespace Apotheca.Api.Events.Notes;

public class NoteDeletedEvent
{
    public const string TopicId = "note-deleted";

    public string NoteId { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public bool IsFolder { get; init; }
    public DateTimeOffset DeletedAt { get; init; }
}
