using Apotheca.Api.Utils;

namespace Apotheca.Api.Features.Notes.GetProjectNotes;

public static class GetProjectNotesMapper
{
    public static GetProjectNotesResponse ToResponse(this ProjectNoteModel model) => new()
    {
        Id                   = model.Id,
        Title                = model.Title,
        Body                 = MarkdownUtils.StripMarkdown(model.Body),
        CreatedBy            = model.CreatedBy,
        CreatedByDisplayName = model.CreatedByDisplayName,
        UpdatedAt            = model.UpdatedAt,
    };

    public static IEnumerable<GetProjectNotesResponse> ToResponse(this IEnumerable<ProjectNoteModel> models) =>
        models.Select(m => m.ToResponse());
}
