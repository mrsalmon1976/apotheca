namespace Apotheca.Api.Features.Projects.GetUserProjects;

public class ProjectStatsModel
{
    public string ProjectId { get; init; } = string.Empty;
    public int OpenTaskCount { get; init; }
    public int MemberCount { get; init; }
}
