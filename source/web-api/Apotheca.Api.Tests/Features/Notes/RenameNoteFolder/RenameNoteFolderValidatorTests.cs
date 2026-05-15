using Apotheca.Api.Features.Notes.RenameNoteFolder;

namespace Apotheca.Api.Tests.Features.Notes.RenameNoteFolder;

[TestFixture]
public class RenameNoteFolderValidatorTests
{
    private RenameNoteFolderValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new RenameNoteFolderValidator();
    }

    private static RenameNoteFolderRequest ValidRequest() => new() { Title = "Meeting Notes" };

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
        var request = new RenameNoteFolderRequest { Title = title };

        var errors = _validator.Validate(request);

        Assert.That(errors, Is.Empty);
    }

    // --- Title required ---

    [Test]
    public void Validate_ReturnsError_WhenTitleIsEmpty()
    {
        var request = new RenameNoteFolderRequest { Title = "" };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.One.EqualTo("Folder name is required."));
    }

    [Test]
    public void Validate_ReturnsError_WhenTitleIsWhitespace()
    {
        var request = new RenameNoteFolderRequest { Title = "   " };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.One.EqualTo("Folder name is required."));
    }

    // --- Minimum length ---

    [TestCase("A")]
    [TestCase("Ab")]     // 2 characters — one short
    public void Validate_ReturnsError_WhenTitleIsTooShort(string title)
    {
        var request = new RenameNoteFolderRequest { Title = title };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.One.EqualTo($"Folder name must be at least {RenameNoteFolderValidator.MinTitleLength} characters."));
    }

    [Test]
    public void Validate_TrimsBeforeCheckingLength()
    {
        // 2 visible chars padded with spaces — should fail length, not required
        var request = new RenameNoteFolderRequest { Title = "  Ab  " };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.One.EqualTo($"Folder name must be at least {RenameNoteFolderValidator.MinTitleLength} characters."));
    }

    [Test]
    public void Validate_ReturnsOnlyOneError_WhenTitleIsEmpty()
    {
        var request = new RenameNoteFolderRequest { Title = "" };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.Count.EqualTo(1));
    }
}
