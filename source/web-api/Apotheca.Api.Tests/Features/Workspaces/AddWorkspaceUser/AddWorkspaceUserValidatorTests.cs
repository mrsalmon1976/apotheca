using Apotheca.Api.Features.Workspaces.AddWorkspaceUser;
using Apotheca.Data;

namespace Apotheca.Api.Tests.Features.Workspaces.AddWorkspaceUser;

[TestFixture]
public class AddWorkspaceUserValidatorTests
{
    private AddWorkspaceUserValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new AddWorkspaceUserValidator();

    [Test]
    public void Validate_ReturnsError_WhenEmailIsEmpty()
    {
        var errors = _validator.Validate(new AddWorkspaceUserRequest { Email = "", WorkspaceRole = DataConstants.WorkspaceRole.Viewer });

        Assert.That(errors, Has.Some.Contains("Email"));
    }

    [Test]
    public void Validate_ReturnsError_WhenEmailIsWhitespace()
    {
        var errors = _validator.Validate(new AddWorkspaceUserRequest { Email = "   ", WorkspaceRole = DataConstants.WorkspaceRole.Viewer });

        Assert.That(errors, Has.Some.Contains("Email"));
    }

    [Test]
    public void Validate_ReturnsError_WhenRoleIsInvalid()
    {
        var errors = _validator.Validate(new AddWorkspaceUserRequest { Email = "a@b.com", WorkspaceRole = "SUPERUSER" });

        Assert.That(errors, Has.Some.Contains("WorkspaceRole"));
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenRoleIsAdmin()
    {
        var errors = _validator.Validate(new AddWorkspaceUserRequest { Email = "a@b.com", WorkspaceRole = DataConstants.WorkspaceRole.Admin });

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenRoleIsViewer()
    {
        var errors = _validator.Validate(new AddWorkspaceUserRequest { Email = "a@b.com", WorkspaceRole = DataConstants.WorkspaceRole.Viewer });

        Assert.That(errors, Is.Empty);
    }
}
