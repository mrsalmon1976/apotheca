namespace Apotheca.Api.Features.ProjectTasks.GetProjectTasks;

public class GetProjectTasksResponse
{
    public string Id { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;
    public string? ParentTaskId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string? AssignedTo { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public DateTimeOffset? DueAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}
