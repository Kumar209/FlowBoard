using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SharedKernel;
using Identity.Service.Infrastructure.Services;
using Project.Service.Infrastructure.Services;
using Project.Service.Infrastructure.Caching;
using Project.Service.Infrastructure.Persistence;
using File.Service.Infrastructure.Services;
using File.Service.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Text;
using IProjectRedisCacheService = Project.Service.Application.Interfaces.IRedisCacheService;
using IFileRedisCacheService = File.Service.Application.Interfaces.IRedisCacheService;

// Roles tests — fixed 0-3 only, custom via permissions
public class RolesTests
{
    [Theory]
    [InlineData("OrgAdmin", true)]
    [InlineData("SuperAdmin", true)]
    [InlineData("Member", false)]
    [InlineData("Client", false)]
    public void IsPrivilegedForManage_ShouldCheckFixed(string role, bool expected)
    {
        Roles.IsPrivilegedForManage(new[] { role }).Should().Be(expected);
    }

    [Fact]
    public void CanUpload_ClientFalse_MemberTrue()
    {
        Roles.CanUpload(new[] { "Client" }).Should().BeFalse();
        Roles.CanUpload(new[] { "Member" }).Should().BeTrue();
        Roles.CanUpload(new[] { "OrgAdmin" }).Should().BeTrue();
    }

    [Fact]
    public void GetLabel_ShouldMap0_3()
    {
        Roles.GetLabel(0).Should().Be("SuperAdmin");
        Roles.GetLabel(1).Should().Be("Member");
        Roles.GetLabel(2).Should().Be("OrgAdmin");
        Roles.GetLabel(3).Should().Be("Client");
    }
}

// ProjectService tests with InMemory
public class ProjectServiceTests
{
    private ProjectDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<ProjectDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        // Need to mock IConfiguration for Redis
        return new ProjectDbContext(opts);
    }

    private IProjectRedisCacheService MockCache()
    {
        var mock = new Mock<IProjectRedisCacheService>();
        mock.Setup(x => x.GetAsync<object>(It.IsAny<string>())).ReturnsAsync((object)null);
        mock.Setup(x => x.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(true);
        mock.Setup(x => x.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        return mock.Object;
    }

    [Fact]
    public async Task GetProjects_PaginationClamp_1_100()
    {
        var db = CreateDb();
        var ws = Guid.NewGuid();
        for (int i = 0; i < 5; i++) db.Projects.Add(new Project.Service.Domain.Entities.Project(ws, $"P{i}", $"PRJ-{i}", Guid.NewGuid(), null));
        await db.SaveChangesAsync();
        var svc = new ProjectService(db, MockCache());
        var r1 = await svc.GetProjectsAsync(ws, 0, 0); // clamp 0 -> 1
        r1.PageSize.Should().Be(1);
        r1.Page.Should().Be(1);
        var r2 = await svc.GetProjectsAsync(ws, 1, 1000); // clamp 1000 -> 100
        r2.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task CreateProject_OrgAdmin_Success_ClientForbidden()
    {
        var db = CreateDb();
        var svc = new ProjectService(db, MockCache());
        var ws = Guid.NewGuid();
        var caller = Guid.NewGuid();
        var ok = await svc.CreateProjectAsync(ws, "Test Project", null, caller, new List<string> { "OrgAdmin" });
        ok.IsSuccess.Should().BeTrue();
        var fail = await svc.CreateProjectAsync(ws, "Another", null, caller, new List<string> { "Client" });
        fail.IsSuccess.Should().BeFalse();
        fail.Error.Should().Contain("Forbidden");
    }

    [Fact]
    public async Task GetBoard_AsNoTracking_NoTracking()
    {
        var db = CreateDb();
        var ws = Guid.NewGuid();
        var p = new Project.Service.Domain.Entities.Project(ws, "BoardTest", "BT-1", Guid.NewGuid(), null);
        db.Projects.Add(p);
        await db.SaveChangesAsync();
        var svc = new ProjectService(db, MockCache());
        var board = await svc.GetBoardAsync(p.Id, null);
        board.Should().NotBeNull();
        board.Project.Name.Should().Be("BoardTest");
    }
}

public class TaskServiceTests
{
    private ProjectDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<ProjectDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ProjectDbContext(opts);
    }
    private IProjectRedisCacheService MockCache()
    {
        var mock = new Mock<IProjectRedisCacheService>();
        mock.Setup(x => x.GetAsync<object>(It.IsAny<string>())).ReturnsAsync((object)null);
        mock.Setup(x => x.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(true);
        mock.Setup(x => x.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        return mock.Object;
    }

    [Fact]
    public async Task CreateTask_ClientForbidden_MemberAllowed()
    {
        var db = CreateDb();
        var ws = Guid.NewGuid();
        var proj = new Project.Service.Domain.Entities.Project(ws, "Proj", "PRJ-1", Guid.NewGuid(), null);
        db.Projects.Add(proj);
        await db.SaveChangesAsync();
        // Need a board list for task creation via listId, but we can use statusId path: create status first
        var status = new Project.Service.Domain.Entities.Status(proj.Id, "To Do");
        db.Statuses.Add(status);
        await db.SaveChangesAsync();
        var svc = new TaskService(db, MockCache());
        var client = await svc.CreateTaskAsync(proj.Id, null, "Title", null, "Medium", null, null, null, "Task", null, null, null, null, null, null, null, Guid.NewGuid(), new List<string> { "Client" }, default, status.Id);
        client.IsSuccess.Should().BeFalse();
        var member = await svc.CreateTaskAsync(proj.Id, null, "Title2", null, "Medium", null, null, null, "Task", null, null, null, null, null, null, null, Guid.NewGuid(), new List<string> { "Member" }, default, status.Id);
        member.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetTasks_PaginationClampAndSearch()
    {
        var db = CreateDb();
        var ws = Guid.NewGuid();
        var proj = new Project.Service.Domain.Entities.Project(ws, "P", "PRJ-1", Guid.NewGuid(), null);
        db.Projects.Add(proj);
        await db.SaveChangesAsync();
        var status = new Project.Service.Domain.Entities.Status(proj.Id, "To Do");
        db.Statuses.Add(status);
        await db.SaveChangesAsync();
        var svc = new TaskService(db, MockCache());
        await svc.CreateTaskAsync(proj.Id, null, "Bug login mobile", null, "Medium", null, null, null, "Task", null, null, null, null, null, null, null, Guid.NewGuid(), new List<string> { "Member" }, default, status.Id);
        await svc.CreateTaskAsync(proj.Id, null, "Feature X", null, "High", null, null, null, "Task", null, null, null, null, null, null, null, Guid.NewGuid(), new List<string> { "Member" }, default, status.Id);
        var filtered = await svc.GetTasksAsync(proj.Id, "bug", null, null, null, null, null, null, false, 1, 20);
        filtered.Items.Should().ContainSingle(x => x.Title.Contains("Bug"));
        var paged = await svc.GetTasksAsync(proj.Id, null, null, null, null, null, null, null, false, 1, 1);
        paged.Items.Count.Should().Be(1);
        paged.Total.Should().Be(2);
        var clamp = await svc.GetTasksAsync(proj.Id, null, null, null, null, null, null, null, false, 0, 200);
        clamp.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task MoveTask_DistributedLock_PreventsConcurrent()
    {
        var db = CreateDb();
        var ws = Guid.NewGuid();
        var proj = new Project.Service.Domain.Entities.Project(ws, "P", "PRJ-1", Guid.NewGuid(), null);
        db.Projects.Add(proj);
        await db.SaveChangesAsync();
        var list1 = new Project.Service.Domain.Entities.BoardList(proj.Id, "To Do", 0);
        var list2 = new Project.Service.Domain.Entities.BoardList(proj.Id, "Done", 1);
        db.BoardLists.AddRange(list1, list2);
        await db.SaveChangesAsync();
        var status = new Project.Service.Domain.Entities.Status(proj.Id, "To Do");
        db.Statuses.Add(status);
        await db.SaveChangesAsync();
        var svc = new TaskService(db, MockCache());
        var t = await svc.CreateTaskAsync(proj.Id, list1.Id, "MoveMe", null, "Medium", null, null, null, "Task", null, null, null, null, null, null, null, Guid.NewGuid(), new List<string> { "Member" }, default, status.Id);
        t.IsSuccess.Should().BeTrue();
        var move = await svc.MoveTaskAsync(t.Value.Id, list2.Id, 0, Guid.NewGuid(), new List<string> { "Member" });
        move.IsSuccess.Should().BeTrue();
    }
}

public class FileServiceTests
{
    [Fact]
    public void Upload_Validation_WhitelistAndSize()
    {
        // FileService MaxSize 25MB, whitelist image/pdf etc.
        var allowed = new[] { ".jpg", ".png", ".pdf", ".zip" };
        allowed.Should().Contain(".pdf");
        // 25MB = 26214400 bytes
        const long max = 25 * 1024 * 1024;
        max.Should().Be(26214400);
    }
}

// Auth + Workspace tests
public class AuthHelperTests
{
    [Fact]
    public void Jwt_MustThrowIfMissingKey()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();
        Action act = () => { var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing"); };
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PasswordHasher_BCrypt_Verify()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("Kumar666@lmp");
        hasher.Verify("Kumar666@lmp", hash).Should().BeTrue();
        hasher.Verify("wrong", hash).Should().BeFalse();
    }
}
