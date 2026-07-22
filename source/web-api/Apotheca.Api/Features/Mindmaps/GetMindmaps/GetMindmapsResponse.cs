namespace Apotheca.Api.Features.Mindmaps.GetMindmaps;

public class GetMindmapsResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; init; }
}
