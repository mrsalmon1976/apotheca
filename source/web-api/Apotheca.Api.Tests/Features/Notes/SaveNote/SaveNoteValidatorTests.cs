using Apotheca.Api.Features.Notes.SaveNote;

namespace Apotheca.Api.Tests.Features.Notes.SaveNote;

[TestFixture]
public class SaveNoteValidatorTests
{
    private SaveNoteValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new SaveNoteValidator();
    }

    // --- All null ---

    [Test]
    public void Validate_ReturnsError_WhenAllFieldsAreNull()
    {
        var request = new SaveNoteRequest();
        var errors = _validator.Validate(request);
        Assert.That(errors, Has.One.EqualTo("At least one field must be provided."));
    }

    // --- Title validation ---

    [Test]
    public void Validate_ReturnsError_WhenTitleIsEmptyString()
    {
        var request = new SaveNoteRequest { Title = "" };
        var errors = _validator.Validate(request);
        Assert.That(errors, Has.One.EqualTo("Title cannot be empty."));
    }

    [Test]
    public void Validate_ReturnsError_WhenTitleIsWhitespace()
    {
        var request = new SaveNoteRequest { Title = "   " };
        var errors = _validator.Validate(request);
        Assert.That(errors, Has.One.EqualTo("Title cannot be empty."));
    }

    [Test]
    public void Validate_ReturnsError_WhenTitleIsTooShort()
    {
        var request = new SaveNoteRequest { Title = "ab" };
        var errors = _validator.Validate(request);
        Assert.That(errors, Has.One.EqualTo($"Title must be at least {SaveNoteValidator.MinTitleLength} characters."));
    }

    [Test]
    public void Validate_ReturnsError_WhenTrimmedTitleIsTooShort()
    {
        var request = new SaveNoteRequest { Title = "  a  " };
        var errors = _validator.Validate(request);
        Assert.That(errors, Has.One.EqualTo($"Title must be at least {SaveNoteValidator.MinTitleLength} characters."));
    }

    // --- Valid cases ---

    [Test]
    public void Validate_ReturnsNoErrors_WhenOnlyTitleIsProvided()
    {
        var request = new SaveNoteRequest { Title = "My Note" };
        var errors = _validator.Validate(request);
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenOnlyBodyIsProvided()
    {
        var request = new SaveNoteRequest { Body = "Some content" };
        var errors = _validator.Validate(request);
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenOnlyLabelsIsProvided()
    {
        var request = new SaveNoteRequest { Labels = ["tag1", "tag2"] };
        var errors = _validator.Validate(request);
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenBodyIsNullAndTitleIsValid()
    {
        var request = new SaveNoteRequest { Title = "Valid Title", Body = null };
        var errors = _validator.Validate(request);
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenAllFieldsAreProvided()
    {
        var request = new SaveNoteRequest { Title = "My Note", Body = "Content", Labels = ["tag"] };
        var errors = _validator.Validate(request);
        Assert.That(errors, Is.Empty);
    }
}
