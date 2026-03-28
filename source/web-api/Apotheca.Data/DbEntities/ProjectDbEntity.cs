namespace Apotheca.Data.DbEntities;

public class ProjectDbEntity
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string ProjectRole { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
