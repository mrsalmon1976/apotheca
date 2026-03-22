using Apotheca.Data;

namespace Apotheca.Api.Features.ProjectTasks.SaveProjectTask;

public class SaveProjectTaskValidator
{
    private static readonly HashSet<string> ValidPriorities =
    [
        DataConstants.TaskPriority.None,
        DataConstants.TaskPriority.Low,
        DataConstants.TaskPriority.Medium,
        DataConstants.TaskPriority.High,
        DataConstants.TaskPriority.Urgent,
    ];

    public virtual IReadOnlyList<string> Validate(SaveProjectTaskRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors.Add("Title is required.");

        if (!ValidPriorities.Contains(request.Priority))
            errors.Add($"Priority must be one of: {string.Join(", ", ValidPriorities)}.");

        return errors;
    }
}
