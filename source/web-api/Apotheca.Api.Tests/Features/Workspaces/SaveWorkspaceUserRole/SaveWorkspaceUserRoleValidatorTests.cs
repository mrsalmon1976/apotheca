using Apotheca.Api.Features.Workspaces.SaveWorkspaceUserRole;
using Apotheca.Data;

namespace Apotheca.Api.Tests.Features.Workspaces.SaveWorkspaceUserRole;

[TestFixture]
public class SaveWorkspaceUserRoleValidatorTests
{
    private SaveWorkspaceUserRoleValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new SaveWorkspaceUserRoleValidator();

    [Test]
    public void Validate_ReturnsError_WhenRoleIsInvalid()
    {
        var errors = _validator.Validate(new SaveWorkspaceUserRoleRequest { WorkspaceRole = "SUPERUSER" });

        Assert.That(errors, Has.Some.Contains("WorkspaceRole"));
    }

    [Test]
    public void Validate_ReturnsError_WhenRoleIsEmpty()
    {
        var errors = _validator.Validate(new SaveWorkspaceUserRoleRequest { WorkspaceRole = "" });

        Assert.That(errors, Has.Some.Contains("WorkspaceRole"));
    }

    [Test]
    public void Validate_ReturnsError_WhenRoleIsContributor()
    {
        // Workspace roles are ADMIN/VIEWER only, unlike project roles - CONTRIBUTOR is invalid here.
        var errors = _validator.Validate(new SaveWorkspaceUserRoleRequest { WorkspaceRole = DataConstants.ProjectRole.Contributor });

        Assert.That(errors, Has.Some.Contains("WorkspaceRole"));
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenRoleIsAdmin()
    {
        var errors = _validator.Validate(new SaveWorkspaceUserRoleRequest { WorkspaceRole = DataConstants.WorkspaceRole.Admin });

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenRoleIsViewer()
    {
        var errors = _validator.Validate(new SaveWorkspaceUserRoleRequest { WorkspaceRole = DataConstants.WorkspaceRole.Viewer });

        Assert.That(errors, Is.Empty);
    }
}
