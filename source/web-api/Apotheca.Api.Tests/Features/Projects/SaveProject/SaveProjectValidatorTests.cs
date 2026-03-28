using Apotheca.Api.Features.Projects.SaveProject;

namespace Apotheca.Api.Tests.Features.Projects.SaveProject;

[TestFixture]
public class SaveProjectValidatorTests
{
    private SaveProjectValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new SaveProjectValidator();
    }

    private static SaveProjectRequest ValidRequest() => new()
    {
        Name = "My Project",
    };

    // --- Valid ---

    [Test]
    public void Validate_ReturnsNoErrors_WhenRequestIsValid()
    {
        var errors = _validator.Validate(ValidRequest());

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenSummaryIsNull()
    {
        var request = new SaveProjectRequest { Name = "My Project", Summary = null };

        var errors = _validator.Validate(request);

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenSummaryIsProvided()
    {
        var request = new SaveProjectRequest { Name = "My Project", Summary = "A brief description." };

        var errors = _validator.Validate(request);

        Assert.That(errors, Is.Empty);
    }

    // --- Name ---

    [Test]
    public void Validate_ReturnsError_WhenNameIsEmpty()
    {
        var request = new SaveProjectRequest { Name = "" };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.One.EqualTo("Name is required."));
    }

    [Test]
    public void Validate_ReturnsError_WhenNameIsWhitespace()
    {
        var request = new SaveProjectRequest { Name = "   " };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.One.EqualTo("Name is required."));
    }
}
