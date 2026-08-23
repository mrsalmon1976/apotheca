using Apotheca.Data;

namespace Apotheca.Api.Features.Projects.SaveProjectUserRole;

public class SaveProjectUserRoleValidator
{
    public virtual IReadOnlyList<string> Validate(SaveProjectUserRoleRequest request)
    {
        var errors = new List<string>();

        if (request.ProjectRole != DataConstants.ProjectRole.Admin &&
            request.ProjectRole != DataConstants.ProjectRole.Contributor &&
            request.ProjectRole != DataConstants.ProjectRole.Viewer)
            errors.Add("ProjectRole must be ADMIN, CONTRIBUTOR, or VIEWER.");

        return errors;
    }
}
