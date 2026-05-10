namespace Apotheca.Api.Events.Documents.DocumentUploaded;

public class DocumentUploadedEvent
{
    public const string TopicId = "document-uploaded";

    public string DocumentId    { get; init; } = string.Empty;
    public string ProjectId     { get; init; } = string.Empty;
    public string BlobReference { get; init; } = string.Empty;
    public string FileExtension { get; init; } = string.Empty;
}
