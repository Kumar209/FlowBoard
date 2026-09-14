using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SharedKernel;
using Project.Service.Infrastructure.Services;
using Project.Service.Infrastructure.Persistence;
using IProjectRedisCacheService = Project.Service.Application.Interfaces.IRedisCacheService;

public class TaskServiceExtraTests
{
    private ProjectDbContext Db() => new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private IProjectRedisCacheService Cache() { var m = new Mock<IProjectRedisCacheService>(); m.Setup(x => x.GetAsync<object>(It.IsAny<string>())).ReturnsAsync((object)null); m.Setup(x => x.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(true); m.Setup(x => x.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true); return m.Object; }

    [Fact]
    public async Task UpdateTask_Success()
    {
        var db = Db(); var ws = Guid.NewGuid(); var proj = new Project.Service.Domain.Entities.Project(ws, "P", "PRJ-1", Guid.NewGuid(), null); db.Projects.Add(proj); await db.SaveChangesAsync();
        var status = new Project.Service.Domain.Entities.Status(proj.Id, "To Do"); db.Statuses.Add(status); await db.SaveChangesAsync();
        var svc = new TaskService(db, Cache());
        var t = await svc.CreateTaskAsync(proj.Id, null, "T", null, "Medium", null, null, null, "Task", null, null, null, null, null, null, null, Guid.NewGuid(), new List<string> { "Member" }, default, status.Id);
        var upd = await svc.UpdateTaskAsync(t.Value.Id, "T2", "desc", "High", null, null, null, "Task", null, null, null, null, null, null, null, null, null, null, null, null, null, null, Guid.NewGuid(), new List<string> { "Member" }, default, status.Id);
        upd.IsSuccess.Should().BeTrue();
        upd.Value.Title.Should().Be("T2");
    }

    [Fact]
    public async Task DeleteTask_Success()
    {
        var db = Db(); var ws = Guid.NewGuid(); var proj = new Project.Service.Domain.Entities.Project(ws, "P", "PRJ-1", Guid.NewGuid(), null); db.Projects.Add(proj); await db.SaveChangesAsync();
        var status = new Project.Service.Domain.Entities.Status(proj.Id, "To Do"); db.Statuses.Add(status); await db.SaveChangesAsync();
        var svc = new TaskService(db, Cache());
        var t = await svc.CreateTaskAsync(proj.Id, null, "T", null, "Medium", null, null, null, "Task", null, null, null, null, null, null, null, Guid.NewGuid(), new List<string> { "Member" }, default, status.Id);
        var del = await svc.DeleteTaskAsync(t.Value.Id, Guid.NewGuid(), new List<string> { "Member" });
        del.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetTaskDetail_Success()
    {
        var db = Db(); var ws = Guid.NewGuid(); var proj = new Project.Service.Domain.Entities.Project(ws, "P", "PRJ-1", Guid.NewGuid(), null); db.Projects.Add(proj); await db.SaveChangesAsync();
        var status = new Project.Service.Domain.Entities.Status(proj.Id, "To Do"); db.Statuses.Add(status); await db.SaveChangesAsync();
        var svc = new TaskService(db, Cache());
        var t = await svc.CreateTaskAsync(proj.Id, null, "T", null, "Medium", null, null, null, "Task", null, null, null, null, null, null, null, Guid.NewGuid(), new List<string> { "Member" }, default, status.Id);
        var detail = await svc.GetTaskDetailAsync(t.Value.Id);
        detail.Should().NotBeNull();
        detail.Task.Title.Should().Be("T");
    }
}

public class StatusServiceExtraTests2
{
    private ProjectDbContext Db() => new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    [Fact]
    public async Task UpdateStatus_Success()
    {
        var db = Db(); var proj = new Project.Service.Domain.Entities.Project(Guid.NewGuid(), "P", "PRJ-1", Guid.NewGuid(), null); db.Projects.Add(proj); await db.SaveChangesAsync();
        var svc = new StatusService(db);
        var c = await svc.CreateStatusAsync(proj.Id, "To Do", Guid.NewGuid(), new List<string> { "OrgAdmin" });
        var u = await svc.UpdateStatusAsync(c.Value.Id, "Doing", Guid.NewGuid(), new List<string> { "OrgAdmin" });
        u.IsSuccess.Should().BeTrue();
        u.Value.Name.Should().Be("Doing");
    }
    [Fact]
    public async Task DeleteStatus_Success()
    {
        var db = Db(); var proj = new Project.Service.Domain.Entities.Project(Guid.NewGuid(), "P", "PRJ-1", Guid.NewGuid(), null); db.Projects.Add(proj); await db.SaveChangesAsync();
        var svc = new StatusService(db);
        var c = await svc.CreateStatusAsync(proj.Id, "Temp", Guid.NewGuid(), new List<string> { "OrgAdmin" });
        var d = await svc.DeleteStatusAsync(c.Value.Id, Guid.NewGuid(), new List<string> { "OrgAdmin" });
        d.IsSuccess.Should().BeTrue();
    }
}

public class BoardServiceExtraTests
{
    private ProjectDbContext Db() => new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private IProjectRedisCacheService Cache() { var m = new Mock<IProjectRedisCacheService>(); m.Setup(x => x.GetAsync<object>(It.IsAny<string>())).ReturnsAsync((object)null); return m.Object; }
    [Fact]
    public async Task UpdateBoard_Success()
    {
        var db = Db(); var proj = new Project.Service.Domain.Entities.Project(Guid.NewGuid(), "P", "PRJ-1", Guid.NewGuid(), null); db.Projects.Add(proj); await db.SaveChangesAsync();
        var svc = new BoardService(db, Cache());
        var c = await svc.CreateBoardAsync(proj.Id, "B1", "Kanban", null, null, Guid.NewGuid(), new List<string> { "OrgAdmin" });
        var u = await svc.UpdateBoardAsync(c.Value.Id, "B2", "Scrum", null, Guid.NewGuid(), new List<string> { "OrgAdmin" });
        u.IsSuccess.Should().BeTrue();
    }
    [Fact]
    public async Task GetBoards_ReturnsAll()
    {
        var db = Db(); var proj = new Project.Service.Domain.Entities.Project(Guid.NewGuid(), "P", "PRJ-1", Guid.NewGuid(), null); db.Projects.Add(proj); await db.SaveChangesAsync();
        var svc = new BoardService(db, Cache());
        await svc.CreateBoardAsync(proj.Id, "B1", "Kanban", null, null, Guid.NewGuid(), new List<string> { "OrgAdmin" });
        await svc.CreateBoardAsync(proj.Id, "B2", "Kanban", null, null, Guid.NewGuid(), new List<string> { "OrgAdmin" });
        var list = await svc.GetBoardsAsync(proj.Id);
        list.Count.Should().Be(2);
    }
}
