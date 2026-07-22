namespace Apotheca.Api.Features.Mindmaps.CreateMindmap;

public class CreateMindmapValidator
{
    public virtual IReadOnlyList<string> Validate(CreateMindmapRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Name is required.");

        return errors;
    }
}
