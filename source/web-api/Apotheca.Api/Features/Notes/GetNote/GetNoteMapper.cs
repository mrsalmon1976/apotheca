using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Features.Notes.GetNote;

public static class GetNoteMapper
{
    public static GetNoteResponse ToResponse(this NoteDbEntity note) => new()
    {
        Id           = note.Id,
        ParentNoteId = note.ParentNoteId,
        IsFolder     = note.IsFolder,
        Title        = note.Title,
        Body         = note.Body,
        CreatedAt    = note.CreatedAt,
        UpdatedAt    = note.UpdatedAt,
    };
}
