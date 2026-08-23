namespace Apotheca.Api.Features.Workspaces.GetWorkspaceUsers;

public class GetWorkspaceUsersResponse
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? PhotoUrl { get; init; }
    public string WorkspaceRole { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
