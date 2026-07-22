namespace Apotheca.Api.Features.Mindmaps.SaveMindmap;

public class SaveMindmapValidator
{
    public virtual IReadOnlyList<string> Validate(SaveMindmapRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Name is required.");

        return errors;
    }
}
