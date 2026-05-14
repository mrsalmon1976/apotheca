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
    public void ToResponse_MapsBody_WhenPlainText()
    {
        var model = new ProjectNoteModel { Body = "Some body content" };

        Assert.That(model.ToResponse().Body, Is.EqualTo("Some body content"));
    }

    [Test]
    public void ToResponse_MapsBody_WhenNull()
    {
        var model = new ProjectNoteModel { Body = null };

        Assert.That(model.ToResponse().Body, Is.Null);
    }

    [Test]
    public void ToResponse_MapsBody_WhenWhitespaceOnly()
    {
        var model = new ProjectNoteModel { Body = "   " };

        Assert.That(model.ToResponse().Body, Is.Null);
    }

    [Test]
    public void ToResponse_Body_StripsBold()
    {
        var model = new ProjectNoteModel { Body = "This is **bold** text" };

        Assert.That(model.ToResponse().Body, Is.EqualTo("This is bold text"));
    }

    [Test]
    public void ToResponse_Body_StripsItalic()
    {
        var model = new ProjectNoteModel { Body = "This is *italic* text" };

        Assert.That(model.ToResponse().Body, Is.EqualTo("This is italic text"));
    }

    [Test]
    public void ToResponse_Body_StripsHeadings()
    {
        var model = new ProjectNoteModel { Body = "## Section heading\nSome content" };

        Assert.That(model.ToResponse().Body, Is.EqualTo("Section heading Some content"));
    }

    [Test]
    public void ToResponse_Body_StripsInlineCode()
    {
        var model = new ProjectNoteModel { Body = "Call `doSomething()` here" };

        Assert.That(model.ToResponse().Body, Is.EqualTo("Call doSomething() here"));
    }

    [Test]
    public void ToResponse_Body_StripsFencedCodeBlock()
    {
        var model = new ProjectNoteModel { Body = "Example:\n```\nvar x = 1;\n```\nDone" };

        Assert.That(model.ToResponse().Body, Is.EqualTo("Example: Done"));
    }

    [Test]
    public void ToResponse_Body_StripsLinks_KeepsText()
    {
        var model = new ProjectNoteModel { Body = "See [the docs](https://example.com) for more" };

        Assert.That(model.ToResponse().Body, Is.EqualTo("See the docs for more"));
    }

    [Test]
    public void ToResponse_Body_StripsImages()
    {
        var model = new ProjectNoteModel { Body = "Here is an image: ![alt text](image.png)" };

        Assert.That(model.ToResponse().Body, Is.EqualTo("Here is an image:"));
    }

    [Test]
    public void ToResponse_Body_StripsBlockquote()
    {
        var model = new ProjectNoteModel { Body = "> This is a quote" };

        Assert.That(model.ToResponse().Body, Is.EqualTo("This is a quote"));
    }

    [Test]
    public void ToResponse_Body_StripsStrikethrough()
    {
        var model = new ProjectNoteModel { Body = "This is ~~wrong~~ right" };

        Assert.That(model.ToResponse().Body, Is.EqualTo("This is wrong right"));
    }

    [Test]
    public void ToResponse_Body_CollapsesNewlines()
    {
        var model = new ProjectNoteModel { Body = "First line\n\nSecond line" };

        Assert.That(model.ToResponse().Body, Is.EqualTo("First line Second line"));
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
