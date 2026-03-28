namespace Apotheca.Api.Features.Projects.SaveProject;

public class SaveProjectValidator
{
    public virtual IReadOnlyList<string> Validate(SaveProjectRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Name is required.");

        return errors;
    }
}
