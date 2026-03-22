using Apotheca.Api.Features.ProjectTasks.SaveProjectTask;

namespace Apotheca.Api.Tests.Features.ProjectTasks.SaveProjectTask;

[TestFixture]
public class SaveProjectTaskValidatorTests
{
    private SaveProjectTaskValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new SaveProjectTaskValidator();
    }

    private static SaveProjectTaskRequest ValidRequest() => new()
    {
        Title    = "A valid title",
        Priority = "NONE",
    };

    // --- Valid ---

    [Test]
    public void Validate_ReturnsNoErrors_WhenRequestIsValid()
    {
        var errors = _validator.Validate(ValidRequest());

        Assert.That(errors, Is.Empty);
    }

    [TestCase("NONE")]
    [TestCase("LOW")]
    [TestCase("MEDIUM")]
    [TestCase("HIGH")]
    [TestCase("URGENT")]
    public void Validate_ReturnsNoErrors_ForAllValidPriorities(string priority)
    {
        var request = new SaveProjectTaskRequest { Title = "Task", Priority = priority };

        var errors = _validator.Validate(request);

        Assert.That(errors, Is.Empty);
    }

    // --- Title ---

    [Test]
    public void Validate_ReturnsError_WhenTitleIsEmpty()
    {
        var request = new SaveProjectTaskRequest { Title = "", Priority = "NONE" };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.One.EqualTo("Title is required."));
    }

    [Test]
    public void Validate_ReturnsError_WhenTitleIsWhitespace()
    {
        var request = new SaveProjectTaskRequest { Title = "   ", Priority = "NONE" };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.One.EqualTo("Title is required."));
    }

    // --- Priority ---

    [Test]
    public void Validate_ReturnsError_WhenPriorityIsInvalid()
    {
        var request = new SaveProjectTaskRequest { Title = "Task", Priority = "INVALID" };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.One.Contain("Priority must be one of:"));
    }

    [Test]
    public void Validate_ReturnsError_WhenPriorityIsLowerCase()
    {
        var request = new SaveProjectTaskRequest { Title = "Task", Priority = "high" };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.One.Contain("Priority must be one of:"));
    }

    // --- Multiple errors ---

    [Test]
    public void Validate_ReturnsMultipleErrors_WhenMultipleFieldsAreInvalid()
    {
        var request = new SaveProjectTaskRequest { Title = "", Priority = "INVALID" };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.Count.EqualTo(2));
    }
}
