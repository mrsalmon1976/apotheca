namespace Apotheca.Data.DbEntities;

public class WorkspaceDbEntity
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string WorkspaceRole { get; init; } = string.Empty;
    public string Plan { get; init; } = string.Empty;
    public string BillingStatus { get; init; } = string.Empty;
    public bool IsCurrent { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
