using Apotheca.Api.Features.Users.GetUserWorkspaces;
using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Tests.Features.Users.GetUserWorkspaces;

[TestFixture]
public class GetUserWorkspacesMapperTests
{
    [Test]
    public void ToResponse_MapsId()
    {
        var entity = new WorkspaceDbEntity { Id = "ws-123" };

        Assert.That(entity.ToResponse().Id, Is.EqualTo("ws-123"));
    }

    [Test]
    public void ToResponse_MapsName()
    {
        var entity = new WorkspaceDbEntity { Name = "Acme Corp" };

        Assert.That(entity.ToResponse().Name, Is.EqualTo("Acme Corp"));
    }

    [Test]
    public void ToResponse_MapsWorkspaceRole()
    {
        var entity = new WorkspaceDbEntity { WorkspaceRole = "ADMIN" };

        Assert.That(entity.ToResponse().WorkspaceRole, Is.EqualTo("ADMIN"));
    }

    [Test]
    public void ToResponse_MapsIsCurrent()
    {
        var entity = new WorkspaceDbEntity { IsCurrent = true };

        Assert.That(entity.ToResponse().IsCurrent, Is.True);
    }

    [Test]
    public void ToResponse_MapsMemberCount_FromStats()
    {
        var entity = new WorkspaceDbEntity();
        var stats  = new WorkspaceStatsModel { MemberCount = 5 };

        Assert.That(entity.ToResponse(stats).MemberCount, Is.EqualTo(5));
    }

    [Test]
    public void ToResponse_MapsProjectCount_FromStats()
    {
        var entity = new WorkspaceDbEntity();
        var stats  = new WorkspaceStatsModel { ProjectCount = 2 };

        Assert.That(entity.ToResponse(stats).ProjectCount, Is.EqualTo(2));
    }

    [Test]
    public void ToResponse_DefaultsCountsToZero_WhenStatsIsNull()
    {
        var entity = new WorkspaceDbEntity();

        var response = entity.ToResponse();

        Assert.That(response.MemberCount, Is.EqualTo(0));
        Assert.That(response.ProjectCount, Is.EqualTo(0));
    }

    [Test]
    public void ToResponse_Collection_MergesMatchingStats()
    {
        var entities = new[]
        {
            new WorkspaceDbEntity { Id = "w1" },
            new WorkspaceDbEntity { Id = "w2" },
        };
        var statsById = new Dictionary<string, WorkspaceStatsModel>
        {
            ["w1"] = new WorkspaceStatsModel { WorkspaceId = "w1", MemberCount = 3, ProjectCount = 1 },
        };

        var responses = entities.ToResponse(statsById).ToList();

        Assert.That(responses[0].MemberCount, Is.EqualTo(3));
        Assert.That(responses[1].MemberCount, Is.EqualTo(0));
    }
}
