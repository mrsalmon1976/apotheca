namespace Apotheca.Api.Features.Projects.GetProjectRecycleBin;

public class GetProjectRecycleBinResponse
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? DeletedBy { get; init; }
    public DateTimeOffset DeletedAt { get; init; }
}
