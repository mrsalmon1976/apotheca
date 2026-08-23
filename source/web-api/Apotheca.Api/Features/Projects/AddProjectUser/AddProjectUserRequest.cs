namespace Apotheca.Api.Features.Projects.AddProjectUser;

public class AddProjectUserRequest
{
    public string UserId { get; init; } = string.Empty;
    public string ProjectRole { get; init; } = string.Empty;
}
