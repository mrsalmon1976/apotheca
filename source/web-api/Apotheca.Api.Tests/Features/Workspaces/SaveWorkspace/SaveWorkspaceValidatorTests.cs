using Apotheca.Api.Features.Workspaces.SaveWorkspace;

namespace Apotheca.Api.Tests.Features.Workspaces.SaveWorkspace;

[TestFixture]
public class SaveWorkspaceValidatorTests
{
    private SaveWorkspaceValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new SaveWorkspaceValidator();

    [Test]
    public void Validate_ReturnsError_WhenNameIsEmpty()
    {
        var errors = _validator.Validate(new SaveWorkspaceRequest { Name = "" });

        Assert.That(errors, Has.Count.EqualTo(1));
    }

    [Test]
    public void Validate_ReturnsError_WhenNameIsWhitespace()
    {
        var errors = _validator.Validate(new SaveWorkspaceRequest { Name = "   " });

        Assert.That(errors, Has.Count.EqualTo(1));
    }

    [Test]
    public void Validate_ReturnsNoErrors_WhenNameIsProvided()
    {
        var errors = _validator.Validate(new SaveWorkspaceRequest { Name = "Acme Corp" });

        Assert.That(errors, Is.Empty);
    }
}
