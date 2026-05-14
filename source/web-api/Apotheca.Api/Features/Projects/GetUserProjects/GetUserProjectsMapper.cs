using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Projects.GetUserProjects;

public static class GetUserProjectsMapper
{
    public static GetUserProjectsResponse ToResponse(this ProjectDbEntity entity, ProjectStatsModel? stats = null) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Summary = entity.Summary,
        ProjectRole = entity.ProjectRole,
        OpenTaskCount = stats?.OpenTaskCount ?? 0,
        MemberCount = stats?.MemberCount ?? 0,
        CreatedAt = entity.CreatedAt,
    };

    public static IEnumerable<GetUserProjectsResponse> ToResponse(
        this IEnumerable<ProjectDbEntity> entities,
        IDictionary<string, ProjectStatsModel> statsById) =>
        entities.Select(e => e.ToResponse(statsById.TryGetValue(e.Id, out var s) ? s : null));
}
