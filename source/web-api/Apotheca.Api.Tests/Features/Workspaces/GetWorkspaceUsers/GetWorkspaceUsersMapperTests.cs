using Apotheca.Api.Features.Workspaces.GetWorkspaceUsers;
using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Tests.Features.Workspaces.GetWorkspaceUsers;

[TestFixture]
public class GetWorkspaceUsersMapperTests
{
    [Test]
    public void ToResponse_MapsUserId()
    {
        var entity = new WorkspaceUserDbEntity { UserId = "user-123" };

        Assert.That(entity.ToResponse().UserId, Is.EqualTo("user-123"));
    }

    [Test]
    public void ToResponse_MapsEmail()
    {
        var entity = new WorkspaceUserDbEntity { Email = "alice@example.com" };

        Assert.That(entity.ToResponse().Email, Is.EqualTo("alice@example.com"));
    }

    [Test]
    public void ToResponse_MapsDisplayName()
    {
        var entity = new WorkspaceUserDbEntity { DisplayName = "Alice" };

        Assert.That(entity.ToResponse().DisplayName, Is.EqualTo("Alice"));
    }

    [Test]
    public void ToResponse_MapsPhotoUrl()
    {
        var entity = new WorkspaceUserDbEntity { PhotoUrl = "https://example.com/photo.png" };

        Assert.That(entity.ToResponse().PhotoUrl, Is.EqualTo("https://example.com/photo.png"));
    }

    [Test]
    public void ToResponse_MapsPhotoUrl_AsNull_WhenNotSet()
    {
        var entity = new WorkspaceUserDbEntity { PhotoUrl = null };

        Assert.That(entity.ToResponse().PhotoUrl, Is.Null);
    }

    [Test]
    public void ToResponse_MapsWorkspaceRole()
    {
        var entity = new WorkspaceUserDbEntity { WorkspaceRole = "ADMIN" };

        Assert.That(entity.ToResponse().WorkspaceRole, Is.EqualTo("ADMIN"));
    }

    [Test]
    public void ToResponse_MapsCreatedAt()
    {
        var createdAt = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var entity = new WorkspaceUserDbEntity { CreatedAt = createdAt };

        Assert.That(entity.ToResponse().CreatedAt, Is.EqualTo(createdAt));
    }

    // --- Collection ---

    [Test]
    public void ToResponse_Collection_MapsAllItems()
    {
        var entities = new[]
        {
            new WorkspaceUserDbEntity { UserId = "u1", DisplayName = "Alice" },
            new WorkspaceUserDbEntity { UserId = "u2", DisplayName = "Bob" },
            new WorkspaceUserDbEntity { UserId = "u3", DisplayName = "Carol" },
        };

        var responses = entities.ToResponse().ToList();

        Assert.That(responses, Has.Count.EqualTo(3));
        Assert.That(responses[0].UserId, Is.EqualTo("u1"));
        Assert.That(responses[1].UserId, Is.EqualTo("u2"));
        Assert.That(responses[2].UserId, Is.EqualTo("u3"));
    }

    [Test]
    public void ToResponse_Collection_ReturnsEmpty_WhenNoEntities()
    {
        var entities = Array.Empty<WorkspaceUserDbEntity>();

        var responses = entities.ToResponse();

        Assert.That(responses, Is.Empty);
    }
}
