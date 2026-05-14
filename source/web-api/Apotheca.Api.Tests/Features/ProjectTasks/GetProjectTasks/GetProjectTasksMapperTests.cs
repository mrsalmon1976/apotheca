using Apotheca.Api.Features.ProjectTasks.GetProjectTasks;

namespace Apotheca.Api.Tests.Features.ProjectTasks.GetProjectTasks;

[TestFixture]
public class GetProjectTasksMapperTests
{
    [Test]
    public void ToResponse_MapsId()
    {
        var model = new ProjectTaskModel { Id = "task-123" };

        Assert.That(model.ToResponse().Id, Is.EqualTo("task-123"));
    }

    [Test]
    public void ToResponse_MapsProjectId()
    {
        var model = new ProjectTaskModel { ProjectId = "proj-456" };

        Assert.That(model.ToResponse().ProjectId, Is.EqualTo("proj-456"));
    }

    [Test]
    public void ToResponse_MapsParentTaskId_WhenSet()
    {
        var model = new ProjectTaskModel { ParentTaskId = "task-parent" };

        Assert.That(model.ToResponse().ParentTaskId, Is.EqualTo("task-parent"));
    }

    [Test]
    public void ToResponse_MapsParentTaskId_WhenNull()
    {
        var model = new ProjectTaskModel { ParentTaskId = null };

        Assert.That(model.ToResponse().ParentTaskId, Is.Null);
    }

    [Test]
    public void ToResponse_MapsTitle()
    {
        var model = new ProjectTaskModel { Title = "Fix the bug" };

        Assert.That(model.ToResponse().Title, Is.EqualTo("Fix the bug"));
    }

    [Test]
    public void ToResponse_MapsNotes_WhenSet()
    {
        var model = new ProjectTaskModel { Notes = "Some notes" };

        Assert.That(model.ToResponse().Notes, Is.EqualTo("Some notes"));
    }

    [Test]
    public void ToResponse_MapsNotes_WhenNull()
    {
        var model = new ProjectTaskModel { Notes = null };

        Assert.That(model.ToResponse().Notes, Is.Null);
    }

    [Test]
    public void ToResponse_MapsAssignedTo_WhenSet()
    {
        var model = new ProjectTaskModel { AssignedTo = "user-789" };

        Assert.That(model.ToResponse().AssignedTo, Is.EqualTo("user-789"));
    }

    [Test]
    public void ToResponse_MapsAssignedTo_WhenNull()
    {
        var model = new ProjectTaskModel { AssignedTo = null };

        Assert.That(model.ToResponse().AssignedTo, Is.Null);
    }

    [Test]
    public void ToResponse_MapsAssignedToDisplayName_WhenSet()
    {
        var model = new ProjectTaskModel { AssignedToDisplayName = "Jane Smith" };

        Assert.That(model.ToResponse().AssignedToDisplayName, Is.EqualTo("Jane Smith"));
    }

    [Test]
    public void ToResponse_MapsAssignedToDisplayName_WhenNull()
    {
        var model = new ProjectTaskModel { AssignedToDisplayName = null };

        Assert.That(model.ToResponse().AssignedToDisplayName, Is.Null);
    }

    [Test]
    public void ToResponse_MapsCreatedBy()
    {
        var model = new ProjectTaskModel { CreatedBy = "user-001" };

        Assert.That(model.ToResponse().CreatedBy, Is.EqualTo("user-001"));
    }

    [Test]
    public void ToResponse_MapsPriority()
    {
        var model = new ProjectTaskModel { Priority = "HIGH" };

        Assert.That(model.ToResponse().Priority, Is.EqualTo("HIGH"));
    }

    [Test]
    public void ToResponse_MapsDueAt_WhenSet()
    {
        var dueAt = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
        var model = new ProjectTaskModel { DueAt = dueAt };

        Assert.That(model.ToResponse().DueAt, Is.EqualTo(dueAt));
    }

    [Test]
    public void ToResponse_MapsDueAt_WhenNull()
    {
        var model = new ProjectTaskModel { DueAt = null };

        Assert.That(model.ToResponse().DueAt, Is.Null);
    }

    [Test]
    public void ToResponse_MapsCreatedAt()
    {
        var createdAt = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var model = new ProjectTaskModel { CreatedAt = createdAt };

        Assert.That(model.ToResponse().CreatedAt, Is.EqualTo(createdAt));
    }

    [Test]
    public void ToResponse_MapsUpdatedAt()
    {
        var updatedAt = new DateTimeOffset(2026, 2, 20, 14, 0, 0, TimeSpan.Zero);
        var model = new ProjectTaskModel { UpdatedAt = updatedAt };

        Assert.That(model.ToResponse().UpdatedAt, Is.EqualTo(updatedAt));
    }

    [Test]
    public void ToResponse_MapsCompletedAt_WhenSet()
    {
        var completedAt = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
        var model = new ProjectTaskModel { CompletedAt = completedAt };

        Assert.That(model.ToResponse().CompletedAt, Is.EqualTo(completedAt));
    }

    [Test]
    public void ToResponse_MapsCompletedAt_WhenNull()
    {
        var model = new ProjectTaskModel { CompletedAt = null };

        Assert.That(model.ToResponse().CompletedAt, Is.Null);
    }

    [Test]
    public void ToResponse_Collection_MapsAllItems()
    {
        var models = new[]
        {
            new ProjectTaskModel { Id = "t1", Title = "Alpha" },
            new ProjectTaskModel { Id = "t2", Title = "Beta" },
            new ProjectTaskModel { Id = "t3", Title = "Gamma" },
        };

        var responses = models.ToResponse().ToList();

        Assert.That(responses, Has.Count.EqualTo(3));
        Assert.That(responses[0].Id, Is.EqualTo("t1"));
        Assert.That(responses[1].Id, Is.EqualTo("t2"));
        Assert.That(responses[2].Id, Is.EqualTo("t3"));
    }
}
