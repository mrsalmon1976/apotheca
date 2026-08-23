namespace Apotheca.Api.Features.Workspaces.AddWorkspaceUser;

public class AddWorkspaceUserRequest
{
    public string Email { get; init; } = string.Empty;
    public string WorkspaceRole { get; init; } = string.Empty;
}
