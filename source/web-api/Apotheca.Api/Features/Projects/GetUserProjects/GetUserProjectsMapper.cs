using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Projects.GetUserProjects;

public static class GetUserProjectsMapper
{
    public static UserProjectsResponse ToResponse(this ProjectDbEntity result) => new()
    {
        Id = result.Id,
        Name = result.Name,
        CreatedAt = result.CreatedAt,
    };

    public static IEnumerable<UserProjectsResponse> ToResponse(this IEnumerable<ProjectDbEntity> results) =>
        results.Select(r => r.ToResponse());
}
