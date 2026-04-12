using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Projects.GetProjectActivity;

public static class GetProjectActivityMapper
{
    public static GetProjectActivityResponse ToResponse(this ProjectActivityLogDbEntity entity) => new()
    {
        Id = entity.Id,
        RefId = entity.RefId,
        RefType = entity.RefType,
        LogMessage = entity.LogMessage,
        Username = entity.Username,
        CreatedAt = entity.CreatedAt,
    };

    public static IEnumerable<GetProjectActivityResponse> ToResponse(this IEnumerable<ProjectActivityLogDbEntity> entities) =>
        entities.Select(e => e.ToResponse());
}
