using System.Net.Http.Json;
using JetBrains.Annotations;
using SolTechnology.Avro;

namespace SolTechnology.Core.HTTP;

public partial class RequestBuilder
{
    /// <summary>
    /// Sends the request and returns the raw <see cref="HttpResponseMessage"/>.
    /// <para>
    /// <strong>Caller owns disposal</strong> — the response holds an open socket / stream
    /// buffer until disposed. Wrap with <c>using</c>:
    /// <code>using var response = await client.CreateRequest("/x").Get(ct);</code>
    /// </para>
    /// <para>
    /// Marked with <see cref="MustDisposeResourceAttribute"/> so Rider / ReSharper /
    /// JetBrains analysers flag callers that drop the returned message — under load
    /// a missed dispose leaks a socket / connection-pool slot until GC.
    /// </para>
    /// </summary>
    [MustDisposeResource]
    public Task<HttpResponseMessage> Get(CancellationToken cancellationToken = default)
        => SendRaw(HttpMethod.Get, cancellationToken);

    /// <inheritdoc cref="Get(CancellationToken)" />
    [MustDisposeResource]
    public Task<HttpResponseMessage> Post(CancellationToken cancellationToken = default)
        => SendRaw(HttpMethod.Post, cancellationToken);

    /// <inheritdoc cref="Get(CancellationToken)" />
    [MustDisposeResource]
    public Task<HttpResponseMessage> Put(CancellationToken cancellationToken = default)
        => SendRaw(HttpMethod.Put, cancellationToken);

    /// <inheritdoc cref="Get(CancellationToken)" />
    [MustDisposeResource]
    public Task<HttpResponseMessage> Patch(CancellationToken cancellationToken = default)
        => SendRaw(HttpMethod.Patch, cancellationToken);

    /// <inheritdoc cref="Get(CancellationToken)" />
    [MustDisposeResource]
    public Task<HttpResponseMessage> Delete(CancellationToken cancellationToken = default)
        => SendRaw(HttpMethod.Delete, cancellationToken);

    /// <summary>
    /// Sends the request and deserialises a success response to <typeparamref name="TResponse"/>.
    /// Throws <see cref="HttpRequestFailedException"/> for a non-success status.
    /// </summary>
    public Task<TResponse> Get<TResponse>(CancellationToken cancellationToken = default)
        => Send<TResponse>(HttpMethod.Get, cancellationToken);

    /// <inheritdoc cref="Get{TResponse}(CancellationToken)" />
    public Task<TResponse> Post<TResponse>(CancellationToken cancellationToken = default)
        => Send<TResponse>(HttpMethod.Post, cancellationToken);

    /// <inheritdoc cref="Get{TResponse}(CancellationToken)" />
    public Task<TResponse> Put<TResponse>(CancellationToken cancellationToken = default)
        => Send<TResponse>(HttpMethod.Put, cancellationToken);

    /// <inheritdoc cref="Get{TResponse}(CancellationToken)" />
    public Task<TResponse> Patch<TResponse>(CancellationToken cancellationToken = default)
        => Send<TResponse>(HttpMethod.Patch, cancellationToken);

    /// <inheritdoc cref="Get{TResponse}(CancellationToken)" />
    public Task<TResponse> Delete<TResponse>(CancellationToken cancellationToken = default)
        => Send<TResponse>(HttpMethod.Delete, cancellationToken);

    private Task<HttpResponseMessage> SendRaw(HttpMethod method, CancellationToken cancellationToken)
    {
        // No using: the caller owns the response, and disposing it also disposes the request.
        var request = BuildRequest(method);
        return _httpClient.SendAsync(request, cancellationToken);
    }

    private async Task<TResponse> Send<TResponse>(HttpMethod method, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(method);
        using var response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.IsSuccessStatusCode == false)
        {
            await ThrowOnFailure(request, response, cancellationToken).ConfigureAwait(false);
        }

        switch (_responseType)
        {
            case DataType.Json:
                return (await response.Content
                    .ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken)
                    .ConfigureAwait(false))!;

            case DataType.Avro:
                var responseAvroContent = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                return AvroConvert.Deserialize<TResponse>(responseAvroContent);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
