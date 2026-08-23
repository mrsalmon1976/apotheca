namespace Apotheca.Api.Features.Users.GetUserWorkspaces;

public class WorkspaceStatsModel
{
    public string WorkspaceId { get; init; } = string.Empty;
    public int MemberCount { get; init; }
    public int ProjectCount { get; init; }
}
