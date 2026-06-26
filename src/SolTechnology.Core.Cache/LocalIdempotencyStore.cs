using System.Collections.Concurrent;

namespace SolTechnology.Core.Cache;

/// <summary>
/// In-process idempotency store backed by <see cref="ConcurrentDictionary{TKey,TValue}"/> with TTL
/// expiry. Suitable for local dev and single-instance deployments. No Redis required.
/// </summary>
internal sealed class LocalIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, Entry> _store = new(StringComparer.Ordinal);
    private readonly TimeSpan _ttl;
    private readonly TimeProvider _timeProvider;

    public LocalIdempotencyStore(TimeSpan ttl, TimeProvider? timeProvider = null)
    {
        _ttl = ttl;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<bool> TryAddAsync(string key, CancellationToken ct = default)
    {
        Evict();
        var entry = new Entry(null, _timeProvider.GetUtcNow());
        var added = _store.TryAdd(key, entry);
        return Task.FromResult(added);
    }

    public Task<StoredResponse?> GetAsync(string key, CancellationToken ct = default)
    {
        if (_store.TryGetValue(key, out var entry) && !IsExpired(entry))
        {
            return Task.FromResult(entry.Response);
        }
        return Task.FromResult<StoredResponse?>(null);
    }

    public Task SetResponseAsync(string key, StoredResponse response, CancellationToken ct = default)
    {
        _store[key] = new Entry(response, _timeProvider.GetUtcNow());
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private bool IsExpired(Entry entry) => _timeProvider.GetUtcNow() - entry.CreatedAt > _ttl;

    private void Evict()
    {
        // Best-effort lazy eviction — remove expired keys on write to prevent unbounded growth.
        foreach (var kvp in _store)
        {
            if (IsExpired(kvp.Value))
            {
                _store.TryRemove(kvp.Key, out _);
            }
        }
    }

    private sealed record Entry(StoredResponse? Response, DateTimeOffset CreatedAt);
}

