namespace Apotheca.Api.Features.Projects.GetUserProjects;

public class GetUserProjectsResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
