using Apotheca.Api.Features.Projects.SaveProjectUserRole;
using Apotheca.Data;

namespace Apotheca.Api.Tests.Features.Projects.SaveProjectUserRole;

[TestFixture]
public class SaveProjectUserRoleValidatorTests
{
    private SaveProjectUserRoleValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new SaveProjectUserRoleValidator();

    [Test]
    public void Validate_ReturnsError_WhenRoleIsInvalid()
    {
        var errors = _validator.Validate(new SaveProjectUserRoleRequest { ProjectRole = "SUPERUSER" });

        Assert.That(errors, Has.Some.Contains("ProjectRole"));
    }

    [Test]
    public void Validate_ReturnsError_WhenRoleIsEmpty()
    {
        var errors = _validator.Validate(new SaveProjectUserRoleRequest { ProjectRole = "" });

        Assert.That(errors, Has.Some.Contains("ProjectRole"));
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenRoleIsAdmin()
    {
        var errors = _validator.Validate(new SaveProjectUserRoleRequest { ProjectRole = DataConstants.ProjectRole.Admin });

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenRoleIsContributor()
    {
        var errors = _validator.Validate(new SaveProjectUserRoleRequest { ProjectRole = DataConstants.ProjectRole.Contributor });

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenRoleIsViewer()
    {
        var errors = _validator.Validate(new SaveProjectUserRoleRequest { ProjectRole = DataConstants.ProjectRole.Viewer });

        Assert.That(errors, Is.Empty);
    }
}
