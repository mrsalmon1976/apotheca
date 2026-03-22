namespace Apotheca.Api.Features.Projects.GetUserProjects;

public class UserProjectsResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
