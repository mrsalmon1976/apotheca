namespace Apotheca.Api.Features.Projects.GetProjectOverview;

public class GetProjectOverviewResponse
{
    public int OpenTaskCount { get; init; }
    public int NoteCount { get; init; }
    public int DocumentCount { get; init; }
}
