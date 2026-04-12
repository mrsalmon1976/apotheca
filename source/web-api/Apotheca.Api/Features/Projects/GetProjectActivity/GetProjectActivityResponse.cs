namespace Apotheca.Api.Features.Projects.GetProjectActivity;

public class GetProjectActivityResponse
{
    public long Id { get; init; }
    public string RefId { get; init; } = string.Empty;
    public string RefType { get; init; } = string.Empty;
    public string LogMessage { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
