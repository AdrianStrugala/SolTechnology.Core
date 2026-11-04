namespace SolTechnology.Core.Cache;

/// <summary>
/// Response stored by the idempotency layer — replayed verbatim on duplicate requests.
/// </summary>
public sealed record StoredResponse
{
    /// <summary>HTTP status code of the original response.</summary>
    public required int StatusCode { get; init; }

    /// <summary>Response headers of the original response.</summary>
    public required Dictionary<string, string[]> Headers { get; init; }

    /// <summary>Raw response body of the original response.</summary>
    public required byte[] Body { get; init; }
}

/// <summary>
/// Idempotency key store — manages the lifecycle of request deduplication keys.
/// <para>
/// Contract:
/// <list type="bullet">
///   <item><see cref="TryAdd"/> atomically reserves a key (returns <c>false</c> if already held).</item>
///   <item><see cref="Get"/> retrieves the stored response for replay.</item>
///   <item><see cref="SetResponse"/> persists the response once the handler completes.</item>
///   <item><see cref="Remove"/> removes the key (e.g. on handler exception — allows retry).</item>
/// </list>
/// </para>
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Atomically reserves a key. Returns <c>true</c> if this caller won the race (key is new);
    /// <c>false</c> if another request already holds the key.
    /// </summary>
    Task<bool> TryAdd(string key, CancellationToken ct = default);

    /// <summary>
    /// Gets the stored response for the key, or <c>null</c> if the response hasn't been persisted
    /// yet (key is reserved but handler hasn't completed).
    /// </summary>
    Task<StoredResponse?> Get(string key, CancellationToken ct = default);

    /// <summary>
    /// Persists the handler's response under the key. Subsequent <see cref="Get"/> calls
    /// return this response for replay.
    /// </summary>
    Task SetResponse(string key, StoredResponse response, CancellationToken ct = default);

    /// <summary>
    /// Removes the key entirely — used when the handler throws (allows the client to retry).
    /// </summary>
    Task Remove(string key, CancellationToken ct = default);
}

