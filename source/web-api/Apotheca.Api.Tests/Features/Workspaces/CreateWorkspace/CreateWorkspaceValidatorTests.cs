using Apotheca.Api.Features.Workspaces.CreateWorkspace;

namespace Apotheca.Api.Tests.Features.Workspaces.CreateWorkspace;

[TestFixture]
public class CreateWorkspaceValidatorTests
{
    private CreateWorkspaceValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateWorkspaceValidator();

    [Test]
    public void Validate_ReturnsError_WhenNameIsEmpty()
    {
        var errors = _validator.Validate(new CreateWorkspaceRequest { Name = "" });

        Assert.That(errors, Has.Count.EqualTo(1));
    }

    [Test]
    public void Validate_ReturnsError_WhenNameIsWhitespace()
    {
        var errors = _validator.Validate(new CreateWorkspaceRequest { Name = "   " });

        Assert.That(errors, Has.Count.EqualTo(1));
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenNameIsProvided()
    {
        var errors = _validator.Validate(new CreateWorkspaceRequest { Name = "Acme Corp" });

        Assert.That(errors, Is.Empty);
    }
}
