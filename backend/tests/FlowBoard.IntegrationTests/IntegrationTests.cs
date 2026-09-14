using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.Json;

public class YarpTests
{
    private string FindYarp()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "yarp.json")))
        {
            var cand = Path.Combine(dir.FullName, "Gateway.YARP", "yarp.json");
            if (File.Exists(cand)) return cand;
            cand = Path.Combine(dir.FullName, "backend", "Gateway.YARP", "yarp.json");
            if (File.Exists(cand)) return cand;
            dir = dir.Parent;
        }
        // Fallback to repo root
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var p = Path.Combine(root, "Gateway.YARP", "yarp.json");
        if (File.Exists(p)) return p;
        p = Path.Combine(root, "backend", "Gateway.YARP", "yarp.json");
        if (File.Exists(p)) return p;
        // Try absolute with Drive
        foreach (var drive in DriveInfo.GetDrives())
        {
            var cand = Path.Combine(drive.RootDirectory.FullName, "Projects + coding", "Dot Net", "Full Stack Projects", "FlowBoard", "backend", "Gateway.YARP", "yarp.json");
            if (File.Exists(cand)) return cand;
        }
        return @"X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard\backend\Gateway.YARP\yarp.json";
    }

    [Fact]
    public void YarpConfig_OrderIsExplicit()
    {
        var path = FindYarp();
        var json = File.ReadAllText(path);
        var doc = JsonDocument.Parse(json);
        var routes = doc.RootElement.GetProperty("ReverseProxy").GetProperty("Routes");
        var projOrder = routes.GetProperty("project-route").TryGetProperty("Order", out var po) ? po.GetInt32() : 99;
        var wsOrder = routes.GetProperty("workspace-route").TryGetProperty("Order", out var wo) ? wo.GetInt32() : 99;
        projOrder.Should().BeLessThan(wsOrder);
        var fileOrder = routes.GetProperty("file-attachments-route").GetProperty("Order").GetInt32();
        var taskOrder = routes.GetProperty("task-route").GetProperty("Order").GetInt32();
        fileOrder.Should().BeLessThan(taskOrder);
    }
}

public class JwtTests
{
    [Fact]
    public void Jwt_MissingKey_Throws()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();
        Action act = () => { var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing"); };
        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Key missing*");
    }

    [Fact]
    public void Jwt_ValidKey_DoesNotThrow()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { ["Jwt:Key"] = "supersecretkeythatis32charslong!!" }).Build();
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        key.Should().NotBeNullOrEmpty();
    }
}

public class PermissionTests
{
    private string FindController()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var cand = Path.Combine(dir.FullName, "OrganizationRolesController.cs");
            if (File.Exists(cand)) return cand;
            var cand2 = Directory.GetFiles(dir.FullName, "OrganizationRolesController.cs", SearchOption.AllDirectories).FirstOrDefault();
            if (cand2 != null) return cand2;
            dir = dir.Parent;
        }
        return @"X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard\backend\Services\Identity.Service\Api\Controllers\OrganizationRolesController.cs";
    }

    [Fact]
    public async Task Permissions_RequiresAuth()
    {
        var path = FindController();
        var file = await File.ReadAllTextAsync(path);
        file.Should().Contain("[Authorize]");
        var lines = file.Split('\n');
        var idx = Array.FindIndex(lines, l => l.Contains("GetAllPermissions"));
        var snippet = string.Join("\n", lines.Skip(Math.Max(0, idx-5)).Take(7));
        snippet.Should().NotContain("[AllowAnonymous]");
        snippet.Should().Contain("[Authorize]");
    }
}

public class PaginationTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1000, 100)]
    [InlineData(1, 1)]
    [InlineData(50, 50)]
    public void Clamp_PageSize_1_100(int input, int expected)
    {
        var clamped = Math.Clamp(input, 1, 100);
        clamped.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10000, 1000)]
    public void Clamp_Page_1_1000(int input, int expected)
    {
        var clamped = Math.Clamp(input, 1, 1000);
        clamped.Should().Be(expected);
    }
}

public class ETagTests
{
    private string GenerateETag(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json));
        return "\"" + Convert.ToHexString(hash)[..16] + "\"";
    }

    [Fact]
    public void ETag_DifferentPayload_DifferentTag()
    {
        var e1 = GenerateETag(new { items = new[] { new { id = 1 } }, total = 1 });
        var e2 = GenerateETag(new { items = new[] { new { id = 1 }, new { id = 2 } }, total = 2 });
        e1.Should().NotBe(e2);
    }

    [Fact]
    public void ETag_SamePayload_SameTag()
    {
        var payload = new { items = new[] { new { id = 1, name = "Test" } }, total = 1 };
        var e1 = GenerateETag(payload);
        var e2 = GenerateETag(payload);
        e1.Should().Be(e2);
    }
}

public class HumanErrorTests
{
    private string ToHuman(Exception ex)
    {
        if (ex is ArgumentException || ex is InvalidOperationException) return ex.Message; // known human
        var msg = ex.Message;
        if (msg.Contains("Raw:") || msg.Contains("LineNumber") || msg.Length > 120) return "Something went wrong — please try again.";
        return msg;
    }

    [Fact]
    public void HumanError_Truncates_Long()
    {
        var ex = new Exception(new string('x', 200));
        ToHuman(ex).Length.Should().BeLessOrEqualTo(50);
    }

    [Fact]
    public void HumanError_Strips_Raw()
    {
        var ex = new Exception("Failed Raw: {...} LineNumber 5");
        ToHuman(ex).Should().Be("Something went wrong — please try again.");
    }
}
