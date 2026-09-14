using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SharedKernel;
using Project.Service.Infrastructure.Services;
using Project.Service.Infrastructure.Persistence;
using Identity.Service.Infrastructure.Persistence;
using Identity.Service.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using IProjectRedisCacheService = Project.Service.Application.Interfaces.IRedisCacheService;

// Additional coverage for 70% — board, sprint, team, comment, subtask, workspace, org
public class BoardServiceTests
{
    private ProjectDbContext Db() => new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private IProjectRedisCacheService Cache() { var m = new Mock<IProjectRedisCacheService>(); m.Setup(x => x.GetAsync<object>(It.IsAny<string>())).ReturnsAsync((object)null); return m.Object; }

    [Fact]
    public async Task CreateBoard_Success()
    {
        var db = Db();
        var proj = new Project.Service.Domain.Entities.Project(Guid.NewGuid(), "P", "PRJ-1", Guid.NewGuid(), null);
        db.Projects.Add(proj); await db.SaveChangesAsync();
        var svc = new BoardService(db, Cache());
        var r = await svc.CreateBoardAsync(proj.Id, "Board1", "Kanban", null, null, Guid.NewGuid(), new List<string> { "OrgAdmin" });
        r.IsSuccess.Should().BeTrue();
    }
}

public class SprintServiceTests
{
    private ProjectDbContext Db() => new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    [Fact]
    public async Task CreateSprint_Success()
    {
        var db = Db();
        var proj = new Project.Service.Domain.Entities.Project(Guid.NewGuid(), "P", "PRJ-1", Guid.NewGuid(), null);
        db.Projects.Add(proj); await db.SaveChangesAsync();
        var svc = new SprintService(db);
        var s = await svc.CreateSprintAsync(proj.Id, null, "Sprint 1", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), Guid.NewGuid(), new List<string> { "OrgAdmin" });
        s.IsSuccess.Should().BeTrue();
    }
}

public class TeamServiceTests
{
    private ProjectDbContext Db() => new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    [Fact]
    public async Task CreateTeam_Success()
    {
        var db = Db();
        var proj = new Project.Service.Domain.Entities.Project(Guid.NewGuid(), "P", "PRJ-1", Guid.NewGuid(), null);
        db.Projects.Add(proj); await db.SaveChangesAsync();
        var svc = new TeamService(db);
        var r = await svc.CreateTeamAsync(proj.Id, "Dev", null, Guid.NewGuid());
        r.IsSuccess.Should().BeTrue();
    }
}

public class CommentServiceTests
{
    private ProjectDbContext Db() => new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private IProjectRedisCacheService Cache() { var m = new Mock<IProjectRedisCacheService>(); m.Setup(x => x.GetAsync<object>(It.IsAny<string>())).ReturnsAsync((object)null); return m.Object; }
    [Fact]
    public async Task AddComment_Success()
    {
        var db = Db();
        var proj = new Project.Service.Domain.Entities.Project(Guid.NewGuid(), "P", "PRJ-1", Guid.NewGuid(), null);
        db.Projects.Add(proj); await db.SaveChangesAsync();
        var status = new Project.Service.Domain.Entities.Status(proj.Id, "To Do");
        db.Statuses.Add(status); await db.SaveChangesAsync();
        var taskSvc = new TaskService(db, Cache());
        var t = await taskSvc.CreateTaskAsync(proj.Id, null, "T", null, "Medium", null, null, null, "Task", null, null, null, null, null, null, null, Guid.NewGuid(), new List<string> { "Member" }, default, status.Id);
        t.IsSuccess.Should().BeTrue();
        var commentSvc = new CommentService(db, Cache());
        var c = await commentSvc.AddCommentAsync(t.Value.Id, "Hello", Guid.NewGuid(), new List<string> { "Member" });
        c.IsSuccess.Should().BeTrue();
    }
}

public class SubTaskServiceTests
{
    private ProjectDbContext Db() => new ProjectDbContext(new DbContextOptionsBuilder<ProjectDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private IProjectRedisCacheService Cache() { var m = new Mock<IProjectRedisCacheService>(); m.Setup(x => x.GetAsync<object>(It.IsAny<string>())).ReturnsAsync((object)null); return m.Object; }
    [Fact]
    public async Task CreateSubTask_Success()
    {
        var db = Db();
        var proj = new Project.Service.Domain.Entities.Project(Guid.NewGuid(), "P", "PRJ-1", Guid.NewGuid(), null);
        db.Projects.Add(proj); await db.SaveChangesAsync();
        var status = new Project.Service.Domain.Entities.Status(proj.Id, "To Do");
        db.Statuses.Add(status); await db.SaveChangesAsync();
        var taskSvc = new TaskService(db, Cache());
        var t = await taskSvc.CreateTaskAsync(proj.Id, null, "T", null, "Medium", null, null, null, "Task", null, null, null, null, null, null, null, Guid.NewGuid(), new List<string> { "Member" }, default, status.Id);
        var subSvc = new SubTaskService(db, Cache());
        var s = await subSvc.CreateSubTaskAsync(t.Value.Id, "Sub1", Guid.NewGuid(), new List<string> { "Member" });
        s.IsSuccess.Should().BeTrue();
        var toggle = await subSvc.ToggleSubTaskAsync(s.Value.Id, Guid.NewGuid());
        toggle.IsSuccess.Should().BeTrue();
    }
}

public class IdentityWorkspaceTests
{
    private Identity.Service.Infrastructure.Persistence.IdentityDbContext Db()
    {
        var opts = new DbContextOptionsBuilder<Identity.Service.Infrastructure.Persistence.IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new Identity.Service.Infrastructure.Persistence.IdentityDbContext(opts);
    }
    [Fact]
    public async Task GetMyWorkspaces_ReturnsEmptyInitially()
    {
        var db = Db();
        var svc = new WorkspaceService(db, new Mock<Identity.Service.Application.Interfaces.IBrevoEmailService>().Object);
        var result = await svc.GetMyWorkspacesAsync(Guid.NewGuid(), 1, 20, null);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Register_CreatesOrgAndWorkspace()
    {
        var db = Db();
        // Seed plan
        var planId = Guid.NewGuid();
        db.SubscriptionPlans.Add(new Identity.Service.Domain.Entities.SubscriptionPlanEntity(planId, "Free", 0, 5, 2, 3, 5, 100, 1000, "[]"));
        await db.SaveChangesAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { ["Jwt:Key"] = "supersecretkeythatis32charslong!!", ["Jwt:Issuer"] = "test", ["Jwt:Audience"] = "test" }).Build();
        var jwt = new JwtProvider(config);
        var hasher = new PasswordHasher();
        var refreshMock = new Mock<Identity.Service.Application.Interfaces.IRefreshTokenService>();
        refreshMock.Setup(x => x.GenerateRawToken()).Returns(("raw", "hash", DateTime.UtcNow.AddDays(7)));
        var platformMock = new Mock<Identity.Service.Application.Interfaces.IPlatformSettingsService>();
        platformMock.Setup(x => x.GetTenantDefaultPlanIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(planId.ToString());
        var auth = new AuthService(db, jwt, refreshMock.Object, hasher, platformMock.Object);
        var res = await auth.RegisterAsync("test@example.com", "Password123!", "Test User", "TestCo", null);
        res.IsSuccess.Should().BeTrue();
        (await db.Users.AnyAsync(u => u.Email == "test@example.com")).Should().BeTrue();
        (await db.Organizations.AnyAsync(o => o.Name == "TestCo")).Should().BeTrue();
    }
}

public class CacheAndRolesTests
{
    [Fact]
    public void CacheKeys_AllGenerate()
    {
        var ws = Guid.NewGuid();
        var proj = Guid.NewGuid();
        Project.Service.Application.Caching.CacheKeys.Board(proj).Should().Contain("board:");
        Project.Service.Application.Caching.CacheKeys.Tasks(proj, "abc").Should().Contain("tasks:");
        Project.Service.Application.Caching.CacheKeys.Projects(ws, 1, 20).Should().Contain("projects:");
    }

    [Fact]
    public void Roles_AllFixedContain()
    {
        Roles.FixedOrgRoles.Should().Contain("Member");
        Roles.FixedOrgRoles.Should().Contain("OrgAdmin");
        Roles.FixedOrgRoles.Should().Contain("Client");
        Roles.AllFixed.Should().Contain("SuperAdmin");
        Roles.IsPrivilegedForManage(new[] { "OrgAdmin" }).Should().BeTrue();
        Roles.IsPrivilegedForManage(new[] { "Member" }).Should().BeFalse();
    }
}
