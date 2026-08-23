using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Projects.GetProjectUsers;

public static class GetProjectUsersMapper
{
    public static GetProjectUsersResponse ToResponse(this ProjectUserDbEntity entity) => new()
    {
        UserId = entity.UserId,
        Email = entity.Email,
        DisplayName = entity.DisplayName,
        PhotoUrl = entity.PhotoUrl,
        ProjectRole = entity.ProjectRole,
        CreatedAt = entity.CreatedAt,
    };

    public static IEnumerable<GetProjectUsersResponse> ToResponse(this IEnumerable<ProjectUserDbEntity> entities) =>
        entities.Select(e => e.ToResponse());
}
