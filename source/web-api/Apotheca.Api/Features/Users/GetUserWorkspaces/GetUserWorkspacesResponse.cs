namespace Apotheca.Api.Features.Users.GetUserWorkspaces;

public class GetUserWorkspacesResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string WorkspaceRole { get; init; } = string.Empty;
    public string Plan { get; init; } = string.Empty;
    public string BillingStatus { get; init; } = string.Empty;
    public int MemberCount { get; init; }
    public int ProjectCount { get; init; }
    public bool IsCurrent { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
