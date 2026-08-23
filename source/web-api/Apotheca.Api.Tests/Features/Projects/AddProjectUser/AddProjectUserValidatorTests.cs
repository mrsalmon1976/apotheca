using Apotheca.Api.Features.Projects.AddProjectUser;
using Apotheca.Data;

namespace Apotheca.Api.Tests.Features.Projects.AddProjectUser;

[TestFixture]
public class AddProjectUserValidatorTests
{
    private AddProjectUserValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new AddProjectUserValidator();

    [Test]
    public void Validate_ReturnsError_WhenUserIdIsEmpty()
    {
        var errors = _validator.Validate(new AddProjectUserRequest { UserId = "", ProjectRole = DataConstants.ProjectRole.Viewer });

        Assert.That(errors, Has.Some.Contains("UserId"));
    }

    [Test]
    public void Validate_ReturnsError_WhenUserIdIsWhitespace()
    {
        var errors = _validator.Validate(new AddProjectUserRequest { UserId = "   ", ProjectRole = DataConstants.ProjectRole.Viewer });

        Assert.That(errors, Has.Some.Contains("UserId"));
    }

    [Test]
    public void Validate_ReturnsError_WhenRoleIsInvalid()
    {
        var errors = _validator.Validate(new AddProjectUserRequest { UserId = "u2", ProjectRole = "SUPERUSER" });

        Assert.That(errors, Has.Some.Contains("ProjectRole"));
    }

    [Test]
    public void Validate_ReturnsBothErrors_WhenUserIdIsEmptyAndRoleIsInvalid()
    {
        var errors = _validator.Validate(new AddProjectUserRequest { UserId = "", ProjectRole = "SUPERUSER" });

        Assert.That(errors, Has.Count.EqualTo(2));
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenRoleIsAdmin()
    {
        var errors = _validator.Validate(new AddProjectUserRequest { UserId = "u2", ProjectRole = DataConstants.ProjectRole.Admin });

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenRoleIsContributor()
    {
        var errors = _validator.Validate(new AddProjectUserRequest { UserId = "u2", ProjectRole = DataConstants.ProjectRole.Contributor });

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenRoleIsViewer()
    {
        var errors = _validator.Validate(new AddProjectUserRequest { UserId = "u2", ProjectRole = DataConstants.ProjectRole.Viewer });

        Assert.That(errors, Is.Empty);
    }
}
