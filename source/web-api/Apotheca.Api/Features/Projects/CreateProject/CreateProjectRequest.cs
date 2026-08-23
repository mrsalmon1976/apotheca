namespace Apotheca.Api.Features.Projects.CreateProject;

public class CreateProjectRequest
{
    public string WorkspaceId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public IReadOnlyList<CreateProjectMemberRequest> Members { get; init; } = [];
}

public class CreateProjectMemberRequest
{
    public string UserId { get; init; } = string.Empty;
    public string ProjectRole { get; init; } = string.Empty;
}
