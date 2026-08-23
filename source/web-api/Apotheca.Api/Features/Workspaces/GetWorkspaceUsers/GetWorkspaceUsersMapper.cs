using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Workspaces.GetWorkspaceUsers;

public static class GetWorkspaceUsersMapper
{
    public static GetWorkspaceUsersResponse ToResponse(this WorkspaceUserDbEntity entity) => new()
    {
        UserId = entity.UserId,
        Email = entity.Email,
        DisplayName = entity.DisplayName,
        PhotoUrl = entity.PhotoUrl,
        WorkspaceRole = entity.WorkspaceRole,
        CreatedAt = entity.CreatedAt,
    };

    public static IEnumerable<GetWorkspaceUsersResponse> ToResponse(this IEnumerable<WorkspaceUserDbEntity> entities) =>
        entities.Select(e => e.ToResponse());
}
