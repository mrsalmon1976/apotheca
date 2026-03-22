namespace Apotheca.Api.Features.ProjectTasks.SaveProjectTask;

public class SaveProjectTaskRequest
{
    public string? Id { get; init; }
    public string? ParentTaskId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string? AssignedTo { get; init; }
    public string Priority { get; init; } = "NONE";
    public DateTimeOffset? DueAt { get; init; }
}
