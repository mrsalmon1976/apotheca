using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Projects.GetUserProjects;

public static class GetUserProjectsMapper
{
    public static GetUserProjectsResponse ToResponse(this ProjectDbEntity result) => new()
    {
        Id = result.Id,
        Name = result.Name,
        CreatedAt = result.CreatedAt,
    };

    public static IEnumerable<GetUserProjectsResponse> ToResponse(this IEnumerable<ProjectDbEntity> results) =>
        results.Select(r => r.ToResponse());
}
