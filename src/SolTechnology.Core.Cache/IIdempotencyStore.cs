namespace SolTechnology.Core.Cache;

/// <summary>
/// Response stored by the idempotency layer — replayed verbatim on duplicate requests.
/// </summary>
public sealed record StoredResponse
{
    public required int StatusCode { get; init; }
    public required Dictionary<string, string[]> Headers { get; init; }
    public required byte[] Body { get; init; }
}

/// <summary>
/// Idempotency key store — manages the lifecycle of request deduplication keys.
/// <para>
/// Contract:
/// <list type="bullet">
///   <item><see cref="TryAddAsync"/> atomically reserves a key (returns <c>false</c> if already held).</item>
///   <item><see cref="GetAsync"/> retrieves the stored response for replay.</item>
///   <item><see cref="SetResponseAsync"/> persists the response once the handler completes.</item>
///   <item><see cref="RemoveAsync"/> removes the key (e.g. on handler exception — allows retry).</item>
/// </list>
/// </para>
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Atomically reserves a key. Returns <c>true</c> if this caller won the race (key is new);
    /// <c>false</c> if another request already holds the key.
    /// </summary>
    Task<bool> TryAddAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Gets the stored response for the key, or <c>null</c> if the response hasn't been persisted
    /// yet (key is reserved but handler hasn't completed).
    /// </summary>
    Task<StoredResponse?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Persists the handler's response under the key. Subsequent <see cref="GetAsync"/> calls
    /// return this response for replay.
    /// </summary>
    Task SetResponseAsync(string key, StoredResponse response, CancellationToken ct = default);

    /// <summary>
    /// Removes the key entirely — used when the handler throws (allows the client to retry).
    /// </summary>
    Task RemoveAsync(string key, CancellationToken ct = default);
}

