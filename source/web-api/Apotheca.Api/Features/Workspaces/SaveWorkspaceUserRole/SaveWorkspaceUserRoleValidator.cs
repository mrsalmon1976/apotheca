using Apotheca.Data;

namespace Apotheca.Api.Features.Workspaces.SaveWorkspaceUserRole;

public class SaveWorkspaceUserRoleValidator
{
    public virtual IReadOnlyList<string> Validate(SaveWorkspaceUserRoleRequest request)
    {
        var errors = new List<string>();

        if (request.WorkspaceRole != DataConstants.WorkspaceRole.Admin &&
            request.WorkspaceRole != DataConstants.WorkspaceRole.Viewer)
            errors.Add("WorkspaceRole must be ADMIN or VIEWER.");

        return errors;
    }
}
