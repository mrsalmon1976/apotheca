namespace Apotheca.Api.Features.Mindmaps.CreateMindmapNode;

public class CreateMindmapNodeRequest
{
    public string ParentNodeId { get; init; } = string.Empty;
    public string? Header { get; init; }
    public string? Body { get; init; }
}
