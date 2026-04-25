using Apotheca.Api.Features.Documents.SaveDocument;

namespace Apotheca.Api.Tests.Features.Documents.SaveDocument;

[TestFixture]
public class SaveDocumentValidatorTests
{
    private SaveDocumentValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new SaveDocumentValidator();
    }

    // --- All null ---

    [Test]
    public void Validate_ReturnsError_WhenAllFieldsAreNull()
    {
        var request = new SaveDocumentRequest();
        var errors  = _validator.Validate(request);
        Assert.That(errors, Has.One.EqualTo("At least one field must be provided."));
    }

    [Test]
    public void Validate_ReturnsOnlyOneError_WhenAllFieldsAreNull()
    {
        var request = new SaveDocumentRequest();
        var errors  = _validator.Validate(request);
        Assert.That(errors, Has.Count.EqualTo(1));
    }

    // --- Title validation ---

    [Test]
    public void Validate_ReturnsError_WhenTitleIsEmptyString()
    {
        var request = new SaveDocumentRequest { Title = "" };
        var errors  = _validator.Validate(request);
        Assert.That(errors, Has.One.EqualTo("Title cannot be empty."));
    }

    [Test]
    public void Validate_ReturnsError_WhenTitleIsWhitespace()
    {
        var request = new SaveDocumentRequest { Title = "   " };
        var errors  = _validator.Validate(request);
        Assert.That(errors, Has.One.EqualTo("Title cannot be empty."));
    }

    // --- Valid cases ---

    [Test]
    public void Validate_ReturnsNoErrors_WhenOnlyTitleIsProvided()
    {
        var request = new SaveDocumentRequest { Title = "spec.pdf" };
        var errors  = _validator.Validate(request);
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenOnlyLabelsIsProvided()
    {
        var request = new SaveDocumentRequest { Labels = ["tag1", "tag2"] };
        var errors  = _validator.Validate(request);
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenBothTitleAndLabelsAreProvided()
    {
        var request = new SaveDocumentRequest { Title = "My Doc", Labels = ["tag"] };
        var errors  = _validator.Validate(request);
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenTitleIsSingleCharacter()
    {
        var request = new SaveDocumentRequest { Title = "a" };
        var errors  = _validator.Validate(request);
        Assert.That(errors, Is.Empty);
    }
}
