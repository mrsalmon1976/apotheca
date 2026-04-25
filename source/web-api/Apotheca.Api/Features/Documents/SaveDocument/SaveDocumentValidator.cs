namespace Apotheca.Api.Features.Documents.SaveDocument;

public class SaveDocumentValidator
{
    public const int MinTitleLength = 1;

    public virtual IReadOnlyList<string> Validate(SaveDocumentRequest request)
    {
        var errors = new List<string>();

        if (request.Title is null && request.Labels is null)
        {
            errors.Add("At least one field must be provided.");
            return errors;
        }

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                errors.Add("Title cannot be empty.");
        }

        return errors;
    }
}
