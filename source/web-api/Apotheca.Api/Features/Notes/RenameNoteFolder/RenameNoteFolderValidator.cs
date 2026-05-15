namespace Apotheca.Api.Features.Notes.RenameNoteFolder;

public class RenameNoteFolderValidator
{
    public const int MinTitleLength = 3;

    public virtual IReadOnlyList<string> Validate(RenameNoteFolderRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors.Add("Folder name is required.");
        else if (request.Title.Trim().Length < MinTitleLength)
            errors.Add($"Folder name must be at least {MinTitleLength} characters.");

        return errors;
    }
}
