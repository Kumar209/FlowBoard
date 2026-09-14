using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SharedKernel;
using Project.Service.Infrastructure.Services;
using Project.Service.Infrastructure.Persistence;
using Project.Service.Infrastructure.Caching;
using File.Service.Infrastructure.Services;
using File.Service.Infrastructure.Persistence;
using Identity.Service.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using IProjectRedisCacheService = Project.Service.Application.Interfaces.IRedisCacheService;

public class StatusServiceTests
{
    private ProjectDbContext Db()
    {
        var opts = new DbContextOptionsBuilder<ProjectDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ProjectDbContext(opts);
    }
    [Fact]
    public async Task CreateStatus_OrgAdmin_Success()
    {
        var db = Db();
        var proj = new Project.Service.Domain.Entities.Project(Guid.NewGuid(), "P", "PRJ-1", Guid.NewGuid(), null);
        db.Projects.Add(proj);
        await db.SaveChangesAsync();
        var svc = new StatusService(db);
        var r1 = await svc.CreateStatusAsync(proj.Id, "To Do", Guid.NewGuid(), new List<string> { "OrgAdmin" });
        r1.IsSuccess.Should().BeTrue();
        r1.Value.Name.Should().Be("To Do");
        var r2 = await svc.CreateStatusAsync(proj.Id, "To Do", Guid.NewGuid(), new List<string> { "OrgAdmin" });
        r2.IsSuccess.Should().BeFalse(); // duplicate
        var r3 = await svc.CreateStatusAsync(proj.Id, "In Progress", Guid.NewGuid(), new List<string> { "Member" });
        r3.IsSuccess.Should().BeFalse(); // Member cannot
    }

    [Fact]
    public async Task GetStatuses_ReturnsAll()
    {
        var db = Db();
        var proj = new Project.Service.Domain.Entities.Project(Guid.NewGuid(), "P", "PRJ-1", Guid.NewGuid(), null);
        db.Projects.Add(proj);
        await db.SaveChangesAsync();
        var svc = new StatusService(db);
        await svc.CreateStatusAsync(proj.Id, "To Do", Guid.NewGuid(), new List<string> { "OrgAdmin" });
        await svc.CreateStatusAsync(proj.Id, "Done", Guid.NewGuid(), new List<string> { "OrgAdmin" });
        var list = await svc.GetStatusesAsync(proj.Id);
        list.Count.Should().Be(2);
    }
}

public class WorkspaceServiceTests
{
    private Identity.Service.Infrastructure.Persistence.IdentityDbContext IdentityDb()
    {
        var opts = new DbContextOptionsBuilder<Identity.Service.Infrastructure.Persistence.IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new Identity.Service.Infrastructure.Persistence.IdentityDbContext(opts);
    }

    [Fact]
    public async Task CreateWorkspace_OrgAdmin_Success()
    {
        var db = IdentityDb();
        var orgId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var planId = Guid.NewGuid();
        db.SubscriptionPlans.Add(new Identity.Service.Domain.Entities.SubscriptionPlanEntity(planId, "Free", 0, 5, 2, 3, 5, 100, 1000, "[]"));
        await db.SaveChangesAsync();
        db.Organizations.Add(new Identity.Service.Domain.Entities.Organization("TestOrg", "test-org", owner, null, planId));
        await db.SaveChangesAsync();
        var org = await db.Organizations.FirstAsync();
        db.OrganizationMembers.Add(new Identity.Service.Domain.Entities.OrganizationMember(org.Id, owner, Roles.OrgAdminValue));
        await db.SaveChangesAsync();
        var brevoMock = new Mock<Identity.Service.Application.Interfaces.IBrevoEmailService>();
        var svc = new WorkspaceService(db, brevoMock.Object);
        var ws = await svc.CreateWorkspaceAsync(org.Id, "General", owner);
        ws.Should().NotBeNull();
        ws.Name.Should().Be("General");
    }
}

public class ProjectServiceExtendedTests
{
    private ProjectDbContext Db()
    {
        var opts = new DbContextOptionsBuilder<ProjectDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ProjectDbContext(opts);
    }
    private IProjectRedisCacheService Cache()
    {
        var m = new Mock<IProjectRedisCacheService>();
        m.Setup(x => x.GetAsync<object>(It.IsAny<string>())).ReturnsAsync((object)null);
        m.Setup(x => x.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(true);
        m.Setup(x => x.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        return m.Object;
    }

    [Fact]
    public async Task UpdateProject_OrgAdmin_Success_MemberForbidden()
    {
        var db = Db();
        var ws = Guid.NewGuid();
        var proj = new Project.Service.Domain.Entities.Project(ws, "Orig", "PRJ-1", Guid.NewGuid(), null);
        db.Projects.Add(proj);
        await db.SaveChangesAsync();
        var svc = new ProjectService(db, Cache());
        var ok = await svc.UpdateProjectAsync(proj.Id, "NewName", "desc", null, Guid.NewGuid(), new List<string> { "OrgAdmin" });
        ok.IsSuccess.Should().BeTrue();
        var fail = await svc.UpdateProjectAsync(proj.Id, "Another", null, null, Guid.NewGuid(), new List<string> { "Member" });
        // Member should be forbidden for update (only OrgAdmin) - but current logic allows only OrgAdmin, so Member should fail
        // Actually Update checks IsPrivilegedForManage, so Member should fail
        fail.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProject_OrgAdmin_Success()
    {
        var db = Db();
        var ws = Guid.NewGuid();
        var proj = new Project.Service.Domain.Entities.Project(ws, "ToDelete", "PRJ-1", Guid.NewGuid(), null);
        db.Projects.Add(proj);
        await db.SaveChangesAsync();
        var svc = new ProjectService(db, Cache());
        var del = await svc.DeleteProjectAsync(proj.Id, Guid.NewGuid(), new List<string> { "OrgAdmin" });
        del.IsSuccess.Should().BeTrue();
        (await db.Projects.AnyAsync(p => p.Id == proj.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task GetBoard_TasksFilteredByTeam()
    {
        var db = Db();
        var ws = Guid.NewGuid();
        var proj = new Project.Service.Domain.Entities.Project(ws, "P", "PRJ-1", Guid.NewGuid(), null);
        db.Projects.Add(proj);
        await db.SaveChangesAsync();
        var status = new Project.Service.Domain.Entities.Status(proj.Id, "To Do");
        db.Statuses.Add(status);
        await db.SaveChangesAsync();
        var svc = new ProjectService(db, Cache());
        var taskSvc = new TaskService(db, Cache());
        await taskSvc.CreateTaskAsync(proj.Id, null, "T1", null, "Medium", null, null, null, "Task", null, null, null, null, null, null, null, Guid.NewGuid(), new List<string> { "Member" }, default, status.Id);
        var boardDto = await svc.GetBoardAsync(proj.Id, null);
        boardDto.Should().NotBeNull();
        boardDto.Tasks.Count.Should().Be(1);
    }
}

public class CacheTests
{
    [Fact]
    public void CacheKeys_GenerateCorrect()
    {
        var ws = Guid.NewGuid();
        var proj = Guid.NewGuid();
        Project.Service.Application.Caching.CacheKeys.Board(proj).Should().Be($"board:{proj}");
        Project.Service.Application.Caching.CacheKeys.Projects(ws, 1, 20).Should().Be($"projects:{ws}:1:20");
    }

    [Fact]
    public async Task RedisCache_NoConnection_NoOp()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { ["Redis:Connection"] = "PASTE_" }).Build();
        var logger = NullLogger<Project.Service.Infrastructure.Caching.RedisCacheService>.Instance;
        var cache = new Project.Service.Infrastructure.Caching.RedisCacheService(config, logger);
        (await cache.GetAsync<string>("key")).Should().BeNull();
        await cache.SetAsync("key", "value", TimeSpan.FromMinutes(1));
        await cache.RemoveAsync("key");
    }
}
