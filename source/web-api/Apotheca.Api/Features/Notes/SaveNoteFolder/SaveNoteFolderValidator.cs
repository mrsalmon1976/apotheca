namespace Apotheca.Api.Features.Notes.SaveNoteFolder;

public class SaveNoteFolderValidator
{
    public const int MinTitleLength = 3;

    public virtual IReadOnlyList<string> Validate(SaveNoteFolderRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors.Add("Folder name is required.");
        else if (request.Title.Trim().Length < MinTitleLength)
            errors.Add($"Folder name must be at least {MinTitleLength} characters.");

        return errors;
    }
}
