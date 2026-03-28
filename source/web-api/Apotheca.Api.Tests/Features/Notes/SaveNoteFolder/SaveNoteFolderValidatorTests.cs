using Apotheca.Api.Features.Notes.SaveNoteFolder;

namespace Apotheca.Api.Tests.Features.Notes.SaveNoteFolder;

[TestFixture]
public class SaveNoteFolderValidatorTests
{
    private SaveNoteFolderValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new SaveNoteFolderValidator();
    }

    private static SaveNoteFolderRequest ValidRequest() => new()
    {
        Title = "Meeting Notes",
    };

    // --- Valid ---

    [Test]
    public void Validate_ReturnsNoErrors_WhenRequestIsValid()
    {
        var errors = _validator.Validate(ValidRequest());

        Assert.That(errors, Is.Empty);
    }

    [TestCase("Hello")]         // exactly 5 characters
    [TestCase("A longer title")]
    public void Validate_ReturnsNoErrors_WhenTitleMeetsMinLength(string title)
    {
        var request = new SaveNoteFolderRequest { Title = title };

        var errors = _validator.Validate(request);

        Assert.That(errors, Is.Empty);
    }

    // --- Title required ---

    [Test]
    public void Validate_ReturnsError_WhenTitleIsEmpty()
    {
        var request = new SaveNoteFolderRequest { Title = "" };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.One.EqualTo("Folder name is required."));
    }

    [Test]
    public void Validate_ReturnsError_WhenTitleIsWhitespace()
    {
        var request = new SaveNoteFolderRequest { Title = "   " };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.One.EqualTo("Folder name is required."));
    }

    // --- Minimum length ---

    [TestCase("A")]
    [TestCase("Ab")]
    [TestCase("Abc")]
    [TestCase("Abcd")]   // 4 characters — one short
    public void Validate_ReturnsError_WhenTitleIsTooShort(string title)
    {
        var request = new SaveNoteFolderRequest { Title = title };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.One.EqualTo($"Folder name must be at least {SaveNoteFolderValidator.MinTitleLength} characters."));
    }

    [Test]
    public void Validate_TrimsBeforeCheckingLength()
    {
        // 4 visible chars padded with spaces — should fail length, not required
        var request = new SaveNoteFolderRequest { Title = "  Ab  " };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.One.EqualTo($"Folder name must be at least {SaveNoteFolderValidator.MinTitleLength} characters."));
    }

    [Test]
    public void Validate_ReturnsOnlyOneError_WhenTitleIsEmpty()
    {
        var request = new SaveNoteFolderRequest { Title = "" };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.Count.EqualTo(1));
    }
}
