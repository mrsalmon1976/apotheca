using Apotheca.Data;

namespace Apotheca.Api.Features.Projects.CreateProject;

public class CreateProjectValidator
{
    public virtual IReadOnlyList<string> Validate(CreateProjectRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.WorkspaceId))
            errors.Add("WorkspaceId is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Name is required.");

        foreach (var member in request.Members)
        {
            if (member.ProjectRole != DataConstants.ProjectRole.Admin &&
                member.ProjectRole != DataConstants.ProjectRole.Contributor &&
                member.ProjectRole != DataConstants.ProjectRole.Viewer)
            {
                errors.Add($"ProjectRole '{member.ProjectRole}' is invalid.");
            }
        }

        return errors;
    }
}
