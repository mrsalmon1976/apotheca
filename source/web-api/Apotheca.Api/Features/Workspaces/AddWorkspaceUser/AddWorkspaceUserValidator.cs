using Apotheca.Data;

namespace Apotheca.Api.Features.Workspaces.AddWorkspaceUser;

public class AddWorkspaceUserValidator
{
    public virtual IReadOnlyList<string> Validate(AddWorkspaceUserRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Email))
            errors.Add("Email is required.");

        if (request.WorkspaceRole != DataConstants.WorkspaceRole.Admin &&
            request.WorkspaceRole != DataConstants.WorkspaceRole.Viewer)
            errors.Add("WorkspaceRole must be ADMIN or VIEWER.");

        return errors;
    }
}
