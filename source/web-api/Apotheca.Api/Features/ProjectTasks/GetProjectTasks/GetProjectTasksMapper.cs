using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.ProjectTasks.GetProjectTasks;

public static class GetProjectTasksMapper
{
    public static GetProjectTasksResponse ToResponse(this TaskDbEntity entity) => new()
    {
        Id           = entity.Id,
        ProjectId    = entity.ProjectId,
        ParentTaskId = entity.ParentTaskId,
        Title        = entity.Title,
        Notes        = entity.Notes,
        AssignedTo   = entity.AssignedTo,
        CreatedBy    = entity.CreatedBy,
        Priority     = entity.Priority,
        DueAt        = entity.DueAt,
        CreatedAt    = entity.CreatedAt,
        UpdatedAt    = entity.UpdatedAt,
    };

    public static IEnumerable<GetProjectTasksResponse> ToResponse(this IEnumerable<TaskDbEntity> entities) =>
        entities.Select(e => e.ToResponse());
}
