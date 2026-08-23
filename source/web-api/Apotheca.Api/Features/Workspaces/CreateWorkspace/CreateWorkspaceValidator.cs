namespace Apotheca.Api.Features.Workspaces.CreateWorkspace;

public class CreateWorkspaceValidator
{
    public virtual IReadOnlyList<string> Validate(CreateWorkspaceRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Name is required.");

        return errors;
    }
}
