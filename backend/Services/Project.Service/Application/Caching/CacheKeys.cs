namespace Project.Service.Application.Caching;

/// <summary>
/// CacheKeys - MNC-grade centralized key factory in Application layer (no Infra dependency). Keeps key format domain-owned, so Api/Application never reference concrete RedisCacheService. Used by controllers to build board:{projectId} 5m and tasks:{projectId}:{hash} 2m keys, and by handlers for invalidation. Single source for TTL constants too.
/// </summary>
public static class CacheKeys
{
    public const int BoardTtlMinutes = 5;
    public const int TasksTtlMinutes = 2;
    public const int ProjectsTtlMinutes = 5;

    public static string Board(Guid projectId) => $"board:{projectId}";
    public static string Tasks(Guid projectId, string hash) => $"tasks:{projectId}:{hash}";
    public static string Projects(Guid workspaceId, int page, int pageSize) => $"projects:{workspaceId}:{page}:{pageSize}";
}
