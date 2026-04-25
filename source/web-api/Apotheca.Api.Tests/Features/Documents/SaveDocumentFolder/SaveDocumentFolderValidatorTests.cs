using Apotheca.Api.Features.Documents.SaveDocumentFolder;

namespace Apotheca.Api.Tests.Features.Documents.SaveDocumentFolder;

[TestFixture]
public class SaveDocumentFolderValidatorTests
{
    private SaveDocumentFolderValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new SaveDocumentFolderValidator();
    }

    private static SaveDocumentFolderRequest ValidRequest() => new() { Title = "Archive" };

    // --- Valid ---

    [Test]
    public void Validate_ReturnsNoErrors_WhenRequestIsValid()
    {
        var errors = _validator.Validate(ValidRequest());
        Assert.That(errors, Is.Empty);
    }

    [TestCase("Abc")]
    [TestCase("A longer title")]
    public void Validate_ReturnsNoErrors_WhenTitleMeetsMinLength(string title)
    {
        var request = new SaveDocumentFolderRequest { Title = title };
        var errors  = _validator.Validate(request);
        Assert.That(errors, Is.Empty);
    }

    // --- Title required ---

    [Test]
    public void Validate_ReturnsError_WhenTitleIsEmpty()
    {
        var request = new SaveDocumentFolderRequest { Title = "" };
        var errors  = _validator.Validate(request);
        Assert.That(errors, Has.One.EqualTo("Folder name is required."));
    }

    [Test]
    public void Validate_ReturnsError_WhenTitleIsWhitespace()
    {
        var request = new SaveDocumentFolderRequest { Title = "   " };
        var errors  = _validator.Validate(request);
        Assert.That(errors, Has.One.EqualTo("Folder name is required."));
    }

    // --- Minimum length ---

    [TestCase("A")]
    [TestCase("Ab")]
    public void Validate_ReturnsError_WhenTitleIsTooShort(string title)
    {
        var request = new SaveDocumentFolderRequest { Title = title };
        var errors  = _validator.Validate(request);
        Assert.That(errors, Has.One.EqualTo($"Folder name must be at least {SaveDocumentFolderValidator.MinTitleLength} characters."));
    }

    [Test]
    public void Validate_TrimsBeforeCheckingLength()
    {
        var request = new SaveDocumentFolderRequest { Title = "  Ab  " };
        var errors  = _validator.Validate(request);
        Assert.That(errors, Has.One.EqualTo($"Folder name must be at least {SaveDocumentFolderValidator.MinTitleLength} characters."));
    }

    [Test]
    public void Validate_ReturnsOnlyOneError_WhenTitleIsEmpty()
    {
        var request = new SaveDocumentFolderRequest { Title = "" };
        var errors  = _validator.Validate(request);
        Assert.That(errors, Has.Count.EqualTo(1));
    }
}
