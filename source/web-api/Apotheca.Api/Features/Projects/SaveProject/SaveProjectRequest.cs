namespace Apotheca.Api.Features.Projects.SaveProject;

public class SaveProjectRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Summary { get; init; }
}
