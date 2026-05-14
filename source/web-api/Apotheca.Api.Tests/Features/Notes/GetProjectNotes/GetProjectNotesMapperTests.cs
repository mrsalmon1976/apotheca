using Apotheca.Api.Features.Notes.GetProjectNotes;

namespace Apotheca.Api.Tests.Features.Notes.GetProjectNotes;

[TestFixture]
public class GetProjectNotesMapperTests
{
    [Test]
    public void ToResponse_MapsId()
    {
        var model = new ProjectNoteModel { Id = "note-123" };

        Assert.That(model.ToResponse().Id, Is.EqualTo("note-123"));
    }

    [Test]
    public void ToResponse_MapsTitle()
    {
        var model = new ProjectNoteModel { Title = "Architecture Notes" };

        Assert.That(model.ToResponse().Title, Is.EqualTo("Architecture Notes"));
    }

    [Test]
    public void ToResponse_MapsCreatedBy()
    {
        var model = new ProjectNoteModel { CreatedBy = "user-001" };

        Assert.That(model.ToResponse().CreatedBy, Is.EqualTo("user-001"));
    }

    [Test]
    public void ToResponse_MapsCreatedByDisplayName_WhenSet()
    {
        var model = new ProjectNoteModel { CreatedByDisplayName = "Jane Smith" };

        Assert.That(model.ToResponse().CreatedByDisplayName, Is.EqualTo("Jane Smith"));
    }

    [Test]
    public void ToResponse_MapsCreatedByDisplayName_WhenNull()
    {
        var model = new ProjectNoteModel { CreatedByDisplayName = null };

        Assert.That(model.ToResponse().CreatedByDisplayName, Is.Null);
    }

    [Test]
    public void ToResponse_MapsUpdatedAt()
    {
        var updatedAt = new DateTimeOffset(2026, 3, 12, 10, 0, 0, TimeSpan.Zero);
        var model = new ProjectNoteModel { UpdatedAt = updatedAt };

        Assert.That(model.ToResponse().UpdatedAt, Is.EqualTo(updatedAt));
    }

    [Test]
    public void ToResponse_Body_WhenPlainText_MapsUnchanged()
    {
        var model = new ProjectNoteModel { Body = "Some body content" };

        Assert.That(model.ToResponse().Body, Is.EqualTo("Some body content"));
    }

    [Test]
    public void ToResponse_Body_WhenNull_ReturnsNull()
    {
        var model = new ProjectNoteModel { Body = null };

        Assert.That(model.ToResponse().Body, Is.Null);
    }

    [Test]
    public void ToResponse_Body_WhenWhitespaceOnly_ReturnsNull()
    {
        var model = new ProjectNoteModel { Body = "   " };

        Assert.That(model.ToResponse().Body, Is.Null);
    }

    [Test]
    public void ToResponse_Body_StripsMarkdown()
    {
        var model = new ProjectNoteModel { Body = "## Heading\nSome **bold** text" };

        Assert.That(model.ToResponse().Body, Is.EqualTo("Heading Some bold text"));
    }

    [Test]
    public void ToResponse_Collection_MapsAllItems()
    {
        var models = new[]
        {
            new ProjectNoteModel { Id = "n1", Title = "First" },
            new ProjectNoteModel { Id = "n2", Title = "Second" },
            new ProjectNoteModel { Id = "n3", Title = "Third" },
        };

        var responses = models.ToResponse().ToList();

        Assert.That(responses, Has.Count.EqualTo(3));
        Assert.That(responses[0].Id, Is.EqualTo("n1"));
        Assert.That(responses[1].Id, Is.EqualTo("n2"));
        Assert.That(responses[2].Id, Is.EqualTo("n3"));
    }
}
