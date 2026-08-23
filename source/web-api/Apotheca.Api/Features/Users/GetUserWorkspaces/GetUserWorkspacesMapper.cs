using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Users.GetUserWorkspaces;

public static class GetUserWorkspacesMapper
{
    public static GetUserWorkspacesResponse ToResponse(this WorkspaceDbEntity entity, WorkspaceStatsModel? stats = null) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        WorkspaceRole = entity.WorkspaceRole,
        Plan = entity.Plan,
        BillingStatus = entity.BillingStatus,
        MemberCount = stats?.MemberCount ?? 0,
        ProjectCount = stats?.ProjectCount ?? 0,
        IsCurrent = entity.IsCurrent,
        CreatedAt = entity.CreatedAt,
    };

    public static IEnumerable<GetUserWorkspacesResponse> ToResponse(
        this IEnumerable<WorkspaceDbEntity> entities,
        IDictionary<string, WorkspaceStatsModel> statsById) =>
        entities.Select(e => e.ToResponse(statsById.TryGetValue(e.Id, out var s) ? s : null));
}
