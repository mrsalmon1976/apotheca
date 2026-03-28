namespace Apotheca.Api.Features.Notes.SaveNote;

public class SaveNoteValidator
{
    public const int MinTitleLength = 3;

    public virtual IReadOnlyList<string> Validate(SaveNoteRequest request)
    {
        var errors = new List<string>();

        if (request.Title is null && request.Body is null && request.Labels is null)
        {
            errors.Add("At least one field must be provided.");
            return errors;
        }

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                errors.Add("Title cannot be empty.");
            else if (request.Title.Trim().Length < MinTitleLength)
                errors.Add($"Title must be at least {MinTitleLength} characters.");
        }

        return errors;
    }
}
