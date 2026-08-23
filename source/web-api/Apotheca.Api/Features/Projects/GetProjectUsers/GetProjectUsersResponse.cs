namespace Apotheca.Api.Features.Projects.GetProjectUsers;

public class GetProjectUsersResponse
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? PhotoUrl { get; init; }
    public string ProjectRole { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
