using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Notes.GetNotes;

public static class GetNotesMapper
{
    public static GetNotesResponse ToResponse(this NoteDbEntity note) => new()
    {
        Id           = note.Id,
        ParentNoteId = note.ParentNoteId,
        IsFolder     = note.IsFolder,
        Title        = note.Title,
        CreatedAt    = note.CreatedAt,
        UpdatedAt    = note.UpdatedAt,
    };

    public static IEnumerable<GetNotesResponse> ToResponse(this IEnumerable<NoteDbEntity> notes) =>
        notes.Select(n => n.ToResponse());
}
