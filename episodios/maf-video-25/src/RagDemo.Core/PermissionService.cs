using Microsoft.Extensions.Caching.Memory;

namespace RagDemo.Core;

public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetAccessibleFileIdsAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> HasAccessAsync(string userId, string fileId, CancellationToken cancellationToken = default);

    Task GrantAccessAsync(string userId, string fileId, CancellationToken cancellationToken = default);

    Task RevokeAccessAsync(string userId, string fileId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Caches the per-user accessible-file list to avoid a permissions round trip on every
/// retrieval, but invalidates that cache entry immediately on grant/revoke so an access
/// change takes effect on the next call rather than waiting out the TTL.
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IVectorStore _vectorStore;
    private readonly IMemoryCache _cache;

    public PermissionService(IVectorStore vectorStore, IMemoryCache cache)
    {
        _vectorStore = vectorStore;
        _cache = cache;
    }

    public Task<IReadOnlyList<string>> GetAccessibleFileIdsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _cache.GetOrCreateAsync(CacheKey(userId), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return _vectorStore.GetAccessibleFileIdsAsync(userId, cancellationToken);
        })!;
    }

    public async Task<bool> HasAccessAsync(string userId, string fileId, CancellationToken cancellationToken = default)
    {
        var fileIds = await GetAccessibleFileIdsAsync(userId, cancellationToken);
        return fileIds.Contains(fileId);
    }

    public async Task GrantAccessAsync(string userId, string fileId, CancellationToken cancellationToken = default)
    {
        await _vectorStore.GrantAccessAsync(userId, fileId, cancellationToken);
        _cache.Remove(CacheKey(userId));
    }

    public async Task RevokeAccessAsync(string userId, string fileId, CancellationToken cancellationToken = default)
    {
        await _vectorStore.RevokeAccessAsync(userId, fileId, cancellationToken);
        _cache.Remove(CacheKey(userId));
    }

    private static string CacheKey(string userId) => $"permissions:{userId}";
}
