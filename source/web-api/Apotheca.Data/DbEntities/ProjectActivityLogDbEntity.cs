namespace Apotheca.Data.DbEntities;

public class ProjectActivityLogDbEntity
{
    public long Id { get; init; }
    public string ProjectId { get; init; } = string.Empty;
    public string RefId { get; init; } = string.Empty;
    public string RefType { get; init; } = string.Empty;
    public string LogMessage { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
