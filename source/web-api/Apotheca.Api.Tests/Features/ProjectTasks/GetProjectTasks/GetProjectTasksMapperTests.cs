using Apotheca.Api.Features.ProjectTasks.GetProjectTasks;
using Apotheca.Data.DbEntities;

namespace Apotheca.Api.Tests.Features.ProjectTasks.GetProjectTasks;

[TestFixture]
public class GetProjectTasksMapperTests
{
    [Test]
    public void ToResponse_MapsId()
    {
        var entity = new TaskDbEntity { Id = "task-123" };

        Assert.That(entity.ToResponse().Id, Is.EqualTo("task-123"));
    }

    [Test]
    public void ToResponse_MapsProjectId()
    {
        var entity = new TaskDbEntity { ProjectId = "proj-456" };

        Assert.That(entity.ToResponse().ProjectId, Is.EqualTo("proj-456"));
    }

    [Test]
    public void ToResponse_MapsParentTaskId_WhenSet()
    {
        var entity = new TaskDbEntity { ParentTaskId = "task-parent" };

        Assert.That(entity.ToResponse().ParentTaskId, Is.EqualTo("task-parent"));
    }

    [Test]
    public void ToResponse_MapsParentTaskId_WhenNull()
    {
        var entity = new TaskDbEntity { ParentTaskId = null };

        Assert.That(entity.ToResponse().ParentTaskId, Is.Null);
    }

    [Test]
    public void ToResponse_MapsTitle()
    {
        var entity = new TaskDbEntity { Title = "Fix the bug" };

        Assert.That(entity.ToResponse().Title, Is.EqualTo("Fix the bug"));
    }

    [Test]
    public void ToResponse_MapsNotes_WhenSet()
    {
        var entity = new TaskDbEntity { Notes = "Some notes" };

        Assert.That(entity.ToResponse().Notes, Is.EqualTo("Some notes"));
    }

    [Test]
    public void ToResponse_MapsNotes_WhenNull()
    {
        var entity = new TaskDbEntity { Notes = null };

        Assert.That(entity.ToResponse().Notes, Is.Null);
    }

    [Test]
    public void ToResponse_MapsAssignedTo_WhenSet()
    {
        var entity = new TaskDbEntity { AssignedTo = "user-789" };

        Assert.That(entity.ToResponse().AssignedTo, Is.EqualTo("user-789"));
    }

    [Test]
    public void ToResponse_MapsAssignedTo_WhenNull()
    {
        var entity = new TaskDbEntity { AssignedTo = null };

        Assert.That(entity.ToResponse().AssignedTo, Is.Null);
    }

    [Test]
    public void ToResponse_MapsCreatedBy()
    {
        var entity = new TaskDbEntity { CreatedBy = "user-001" };

        Assert.That(entity.ToResponse().CreatedBy, Is.EqualTo("user-001"));
    }

    [Test]
    public void ToResponse_MapsPriority()
    {
        var entity = new TaskDbEntity { Priority = "HIGH" };

        Assert.That(entity.ToResponse().Priority, Is.EqualTo("HIGH"));
    }

    [Test]
    public void ToResponse_MapsDueAt_WhenSet()
    {
        var dueAt = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
        var entity = new TaskDbEntity { DueAt = dueAt };

        Assert.That(entity.ToResponse().DueAt, Is.EqualTo(dueAt));
    }

    [Test]
    public void ToResponse_MapsDueAt_WhenNull()
    {
        var entity = new TaskDbEntity { DueAt = null };

        Assert.That(entity.ToResponse().DueAt, Is.Null);
    }

    [Test]
    public void ToResponse_MapsCreatedAt()
    {
        var createdAt = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var entity = new TaskDbEntity { CreatedAt = createdAt };

        Assert.That(entity.ToResponse().CreatedAt, Is.EqualTo(createdAt));
    }

    [Test]
    public void ToResponse_MapsUpdatedAt()
    {
        var updatedAt = new DateTimeOffset(2026, 2, 20, 14, 0, 0, TimeSpan.Zero);
        var entity = new TaskDbEntity { UpdatedAt = updatedAt };

        Assert.That(entity.ToResponse().UpdatedAt, Is.EqualTo(updatedAt));
    }

    [Test]
    public void ToResponse_MapsCompletedAt_WhenSet()
    {
        var completedAt = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
        var entity = new TaskDbEntity { CompletedAt = completedAt };

        Assert.That(entity.ToResponse().CompletedAt, Is.EqualTo(completedAt));
    }

    [Test]
    public void ToResponse_MapsCompletedAt_WhenNull()
    {
        var entity = new TaskDbEntity { CompletedAt = null };

        Assert.That(entity.ToResponse().CompletedAt, Is.Null);
    }

    [Test]
    public void ToResponse_Collection_MapsAllItems()
    {
        var entities = new[]
        {
            new TaskDbEntity { Id = "t1", Title = "Alpha" },
            new TaskDbEntity { Id = "t2", Title = "Beta" },
            new TaskDbEntity { Id = "t3", Title = "Gamma" },
        };

        var responses = entities.ToResponse().ToList();

        Assert.That(responses, Has.Count.EqualTo(3));
        Assert.That(responses[0].Id, Is.EqualTo("t1"));
        Assert.That(responses[1].Id, Is.EqualTo("t2"));
        Assert.That(responses[2].Id, Is.EqualTo("t3"));
    }
}
