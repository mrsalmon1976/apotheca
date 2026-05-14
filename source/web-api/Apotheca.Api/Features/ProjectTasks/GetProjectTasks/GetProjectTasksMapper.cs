namespace Apotheca.Api.Features.ProjectTasks.GetProjectTasks;

public static class GetProjectTasksMapper
{
    public static GetProjectTasksResponse ToResponse(this ProjectTaskModel model) => new()
    {
        Id                    = model.Id,
        ProjectId             = model.ProjectId,
        ParentTaskId          = model.ParentTaskId,
        Title                 = model.Title,
        Notes                 = model.Notes,
        AssignedTo            = model.AssignedTo,
        AssignedToDisplayName = model.AssignedToDisplayName,
        CreatedBy             = model.CreatedBy,
        Priority              = model.Priority,
        DueAt                 = model.DueAt,
        CreatedAt             = model.CreatedAt,
        UpdatedAt             = model.UpdatedAt,
        CompletedAt           = model.CompletedAt,
    };

    public static IEnumerable<GetProjectTasksResponse> ToResponse(this IEnumerable<ProjectTaskModel> models) =>
        models.Select(m => m.ToResponse());
}
