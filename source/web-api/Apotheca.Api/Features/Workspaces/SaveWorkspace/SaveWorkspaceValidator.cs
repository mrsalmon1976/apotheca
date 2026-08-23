namespace Apotheca.Api.Features.Workspaces.SaveWorkspace;

public class SaveWorkspaceValidator
{
    public virtual IReadOnlyList<string> Validate(SaveWorkspaceRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Name is required.");

        return errors;
    }
}
