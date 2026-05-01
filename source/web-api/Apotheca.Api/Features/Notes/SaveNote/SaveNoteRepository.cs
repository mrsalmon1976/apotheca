using Apotheca.Data;
using NanoidDotNet;

namespace Apotheca.Api.Features.Notes.SaveNote;

public class SaveNoteRepository
{
    public virtual async Task DeleteNoteLabelsAsync(IDbContext db, string noteId)
    {
        await db.ExecuteAsync(
            "DELETE FROM note_labels WHERE note_id = @NoteId",
            new { NoteId = noteId });
    }

    public virtual async Task<(string Title, string Body)> GetNoteTitleBodyAsync(IDbContext db, string noteId)
    {
        var note = await db.QueryFirstOrDefaultAsync<NoteContent>(
            "SELECT title, body FROM notes WHERE id = @NoteId",
            new { NoteId = noteId });
        return (note?.Title ?? string.Empty, note?.Body ?? string.Empty);
    }

    public virtual async Task InsertNoteLabelAsync(IDbContext db, string noteId, string labelId)
    {
        await db.ExecuteAsync(
            "INSERT INTO note_labels (note_id, label_id) VALUES (@NoteId, @LabelId) ON CONFLICT DO NOTHING",
            new { NoteId = noteId, LabelId = labelId });
    }

    public virtual async Task<bool> NoteExistsAsync(IDbContext db, string projectId, string noteId)
    {
        var count = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM notes WHERE id = @NoteId AND project_id = @ProjectId AND is_folder = FALSE",
            new { NoteId = noteId, ProjectId = projectId });
        return count > 0;
    }

    public virtual async Task UpdateNoteCoreAsync(
        IDbContext db, string projectId, string noteId, string? title, string? body)
    {
        await db.ExecuteAsync(
            @"UPDATE notes
              SET title      = COALESCE(@Title, title),
                  body       = COALESCE(@Body, body),
                  updated_at = now()
              WHERE id = @NoteId
                AND project_id = @ProjectId",
            new { Title = title, Body = body, NoteId = noteId, ProjectId = projectId });
    }

    public virtual async Task<string> UpsertLabelAsync(
        IDbContext db, string projectId, string userId, string labelText)
    {
        await db.ExecuteAsync(
            @"INSERT INTO labels (id, project_id, label_text, created_by)
              VALUES (@Id, @ProjectId, @LabelText, @CreatedBy)
              ON CONFLICT (project_id, label_text) DO NOTHING",
            new
            {
                Id        = Nanoid.Generate(),
                ProjectId = projectId,
                LabelText = labelText,
                CreatedBy = userId,
            });

        return (await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT id FROM labels WHERE project_id = @ProjectId AND label_text = @LabelText",
            new { ProjectId = projectId, LabelText = labelText }))!;
    }

    public virtual async Task UpsertSearchAsync(IDbContext db, string projectId, string noteId, string title, string body)
    {
        await db.ExecuteAsync(
            @"INSERT INTO search (reference_id, reference_type, project_id, text_title, text_body, updated_at)
              VALUES (@ReferenceId, 'note', @ProjectId, @Title, @Body, now())
              ON CONFLICT (reference_id, reference_type) DO UPDATE
              SET project_id = EXCLUDED.project_id,
                  text_title = EXCLUDED.text_title,
                  text_body  = EXCLUDED.text_body,
                  updated_at = now()",
            new { ReferenceId = noteId, ProjectId = projectId, Title = title, Body = body });
    }

    private record NoteContent(string Title, string? Body);
}
