using Apotheca.Api.Features.Projects.CreateProject;
using Apotheca.Data;

namespace Apotheca.Api.Tests.Features.Projects.CreateProject;

[TestFixture]
public class CreateProjectValidatorTests
{
    private CreateProjectValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateProjectValidator();

    [Test]
    public void Validate_ReturnsError_WhenWorkspaceIdIsEmpty()
    {
        var errors = _validator.Validate(new CreateProjectRequest { WorkspaceId = "", Name = "Project" });

        Assert.That(errors, Has.Some.Contains("WorkspaceId"));
    }

    [Test]
    public void Validate_ReturnsError_WhenNameIsEmpty()
    {
        var errors = _validator.Validate(new CreateProjectRequest { WorkspaceId = "ws-1", Name = "" });

        Assert.That(errors, Has.Some.Contains("Name"));
    }

    [Test]
    public void Validate_ReturnsError_WhenNameIsWhitespace()
    {
        var errors = _validator.Validate(new CreateProjectRequest { WorkspaceId = "ws-1", Name = "   " });

        Assert.That(errors, Has.Some.Contains("Name"));
    }

    [Test]
    public void Validate_ReturnsBothErrors_WhenWorkspaceIdAndNameAreEmpty()
    {
        var errors = _validator.Validate(new CreateProjectRequest { WorkspaceId = "", Name = "" });

        Assert.That(errors, Has.Count.EqualTo(2));
    }

    [Test]
    public void Validate_ReturnsError_WhenMemberRoleIsInvalid()
    {
        var request = new CreateProjectRequest
        {
            WorkspaceId = "ws-1",
            Name = "Project",
            Members = [new CreateProjectMemberRequest { UserId = "u1", ProjectRole = "OWNER" }],
        };

        var errors = _validator.Validate(request);

        Assert.That(errors, Has.Some.Contains("ProjectRole"));
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenRequestIsValid()
    {
        var request = new CreateProjectRequest
        {
            WorkspaceId = "ws-1",
            Name = "Project",
            Members = [new CreateProjectMemberRequest { UserId = "u1", ProjectRole = DataConstants.ProjectRole.Contributor }],
        };

        var errors = _validator.Validate(request);

        Assert.That(errors, Is.Empty);
    }
}
