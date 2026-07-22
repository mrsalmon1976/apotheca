namespace Apotheca.Api.Features.Mindmaps.SaveMindmapNode;

public class SaveMindmapNodeValidator
{
    public virtual IReadOnlyList<string> Validate(SaveMindmapNodeRequest request)
    {
        var errors = new List<string>();

        if (request.Header is null && request.Body is null && request.Collapsed is null)
        {
            errors.Add("At least one field must be provided.");
            return errors;
        }

        if (request.Header is not null && string.IsNullOrWhiteSpace(request.Header))
            errors.Add("Header cannot be empty.");

        return errors;
    }
}
