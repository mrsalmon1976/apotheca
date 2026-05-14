using Apotheca.Api.Features.Projects.GetUserProjects;
using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Tests.Features.Projects.GetUserProjects;

[TestFixture]
public class GetUserProjectsMapperTests
{
    // --- Entity fields ---

    [Test]
    public void ToResponse_MapsId()
    {
        var entity = new ProjectDbEntity { Id = "proj-123" };

        Assert.That(entity.ToResponse().Id, Is.EqualTo("proj-123"));
    }

    [Test]
    public void ToResponse_MapsName()
    {
        var entity = new ProjectDbEntity { Name = "My Project" };

        Assert.That(entity.ToResponse().Name, Is.EqualTo("My Project"));
    }

    [Test]
    public void ToResponse_MapsSummary()
    {
        var entity = new ProjectDbEntity { Summary = "A short description." };

        Assert.That(entity.ToResponse().Summary, Is.EqualTo("A short description."));
    }

    [Test]
    public void ToResponse_MapsSummary_AsNull_WhenNotSet()
    {
        var entity = new ProjectDbEntity { Summary = null };

        Assert.That(entity.ToResponse().Summary, Is.Null);
    }

    [Test]
    public void ToResponse_MapsProjectRole()
    {
        var entity = new ProjectDbEntity { ProjectRole = "owner" };

        Assert.That(entity.ToResponse().ProjectRole, Is.EqualTo("owner"));
    }

    [Test]
    public void ToResponse_MapsCreatedAt()
    {
        var createdAt = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var entity = new ProjectDbEntity { CreatedAt = createdAt };

        Assert.That(entity.ToResponse().CreatedAt, Is.EqualTo(createdAt));
    }

    // --- Stats fields ---

    [Test]
    public void ToResponse_MapsOpenTaskCount_FromStats()
    {
        var entity = new ProjectDbEntity();
        var stats  = new ProjectStatsModel { OpenTaskCount = 7 };

        Assert.That(entity.ToResponse(stats).OpenTaskCount, Is.EqualTo(7));
    }

    [Test]
    public void ToResponse_MapsMemberCount_FromStats()
    {
        var entity = new ProjectDbEntity();
        var stats  = new ProjectStatsModel { MemberCount = 4 };

        Assert.That(entity.ToResponse(stats).MemberCount, Is.EqualTo(4));
    }

    [Test]
    public void ToResponse_DefaultsOpenTaskCountToZero_WhenStatsIsNull()
    {
        var entity = new ProjectDbEntity();

        Assert.That(entity.ToResponse().OpenTaskCount, Is.EqualTo(0));
    }

    [Test]
    public void ToResponse_DefaultsMemberCountToZero_WhenStatsIsNull()
    {
        var entity = new ProjectDbEntity();

        Assert.That(entity.ToResponse().MemberCount, Is.EqualTo(0));
    }

    // --- Collection ---

    [Test]
    public void ToResponse_Collection_MapsAllItems()
    {
        var entities = new[]
        {
            new ProjectDbEntity { Id = "p1", Name = "Alpha" },
            new ProjectDbEntity { Id = "p2", Name = "Beta" },
            new ProjectDbEntity { Id = "p3", Name = "Gamma" },
        };
        var statsById = new Dictionary<string, ProjectStatsModel>();

        var responses = entities.ToResponse(statsById).ToList();

        Assert.That(responses, Has.Count.EqualTo(3));
        Assert.That(responses[0].Id, Is.EqualTo("p1"));
        Assert.That(responses[1].Id, Is.EqualTo("p2"));
        Assert.That(responses[2].Id, Is.EqualTo("p3"));
    }

    [Test]
    public void ToResponse_Collection_MergesMatchingStats()
    {
        var entities = new[]
        {
            new ProjectDbEntity { Id = "p1", Name = "Alpha" },
            new ProjectDbEntity { Id = "p2", Name = "Beta" },
        };
        var statsById = new Dictionary<string, ProjectStatsModel>
        {
            ["p1"] = new ProjectStatsModel { ProjectId = "p1", OpenTaskCount = 5, MemberCount = 3 },
        };

        var responses = entities.ToResponse(statsById).ToList();

        Assert.That(responses[0].OpenTaskCount, Is.EqualTo(5));
        Assert.That(responses[0].MemberCount,   Is.EqualTo(3));
        Assert.That(responses[1].OpenTaskCount, Is.EqualTo(0));
        Assert.That(responses[1].MemberCount,   Is.EqualTo(0));
    }
}
