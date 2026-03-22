using Apotheca.Api.Features.Projects.GetUserProjects;
using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Tests.Features.Projects.GetUserProjects;

[TestFixture]
public class GetUserProjectsMapperTests
{
    [Test]
    public void ToResponse_MapsId()
    {
        var result = new ProjectDbEntity { Id = "proj-123" };

        Assert.That(result.ToResponse().Id, Is.EqualTo("proj-123"));
    }

    [Test]
    public void ToResponse_MapsName()
    {
        var result = new ProjectDbEntity { Name = "My Project" };

        Assert.That(result.ToResponse().Name, Is.EqualTo("My Project"));
    }

    [Test]
    public void ToResponse_MapsCreatedAt()
    {
        var createdAt = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var result = new ProjectDbEntity { CreatedAt = createdAt };

        Assert.That(result.ToResponse().CreatedAt, Is.EqualTo(createdAt));
    }

    [Test]
    public void ToResponse_Collection_MapsAllItems()
    {
        var results = new[]
        {
            new ProjectDbEntity { Id = "p1", Name = "Alpha" },
            new ProjectDbEntity { Id = "p2", Name = "Beta" },
            new ProjectDbEntity { Id = "p3", Name = "Gamma" },
        };

        var responses = results.ToResponse().ToList();

        Assert.That(responses, Has.Count.EqualTo(3));
        Assert.That(responses[0].Id, Is.EqualTo("p1"));
        Assert.That(responses[1].Id, Is.EqualTo("p2"));
        Assert.That(responses[2].Id, Is.EqualTo("p3"));
    }
}
