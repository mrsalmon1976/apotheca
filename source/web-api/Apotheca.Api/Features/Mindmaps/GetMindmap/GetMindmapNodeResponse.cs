namespace Apotheca.Api.Features.Mindmaps.GetMindmap;

public class GetMindmapNodeResponse
{
    public string Id { get; init; } = string.Empty;
    public string? ParentNodeId { get; init; }
    public string Header { get; init; } = string.Empty;
    public string? Body { get; init; }
    public bool Collapsed { get; init; }
    public int Position { get; init; }
}
