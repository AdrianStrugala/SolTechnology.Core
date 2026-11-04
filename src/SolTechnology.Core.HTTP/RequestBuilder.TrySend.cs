using System.Net.Http.Json;
using SolTechnology.Avro;

namespace SolTechnology.Core.HTTP;

public partial class RequestBuilder
{
    /// <summary>
    /// Same as <see cref="Get{TResponse}"/> but returns <see cref="Result{T}"/> instead of
    /// throwing. Failures are mapped onto Core <see cref="Error"/> subtypes via
    /// <see cref="ServiceCallErrorMapper"/>.
    /// </summary>
    public Task<Result<TResponse>> TryGet<TResponse>(CancellationToken cancellationToken = default)
        => TrySend<TResponse>(HttpMethod.Get, cancellationToken);

    /// <inheritdoc cref="TryGet{TResponse}"/>
    public Task<Result<TResponse>> TryPost<TResponse>(CancellationToken cancellationToken = default)
        => TrySend<TResponse>(HttpMethod.Post, cancellationToken);

    /// <inheritdoc cref="TryGet{TResponse}"/>
    public Task<Result<TResponse>> TryPut<TResponse>(CancellationToken cancellationToken = default)
        => TrySend<TResponse>(HttpMethod.Put, cancellationToken);

    /// <inheritdoc cref="TryGet{TResponse}"/>
    public Task<Result<TResponse>> TryPatch<TResponse>(CancellationToken cancellationToken = default)
        => TrySend<TResponse>(HttpMethod.Patch, cancellationToken);

    /// <inheritdoc cref="TryGet{TResponse}"/>
    public Task<Result<TResponse>> TryDelete<TResponse>(CancellationToken cancellationToken = default)
        => TrySend<TResponse>(HttpMethod.Delete, cancellationToken);

    /// <summary>
    /// Railway-style alternative to <see cref="Send{TResponse}"/>: returns <see cref="Result{T}"/>
    /// instead of throwing. Maps transport, status, and deserialisation failures onto Core
    /// <see cref="Error"/> subtypes via <see cref="ServiceCallErrorMapper"/>.
    /// </summary>
    private async Task<Result<TResponse>> TrySend<TResponse>(HttpMethod method, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            using var request = BuildRequest(method);
            response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Result<TResponse>.Fail(ServiceCallErrorMapper.FromException(ex));
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await TryReadCappedBody(response, cancellationToken).ConfigureAwait(false);
                return Result<TResponse>.Fail(ServiceCallErrorMapper.FromStatusCode(response.StatusCode, body));
            }

            try
            {
                return await ReadSuccess<TResponse>(response, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var body = await TryReadCappedBody(response, cancellationToken).ConfigureAwait(false);
                return Result<TResponse>.Fail(ServiceCallErrorMapper.FromDeserializationFailure(ex, body));
            }
        }
    }

    private async Task<Result<TResponse>> ReadSuccess<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        switch (_responseType)
        {
            case DataType.Json:
                var result = await response.Content
                    .ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
                return Result<TResponse>.Success(result!);

            case DataType.Avro:
                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                return Result<TResponse>.Success(AvroConvert.Deserialize<TResponse>(bytes));

            default:
                return Result<TResponse>.Fail(new Error
                {
                    Message = "Unsupported response type",
                    Recoverable = false
                });
        }
    }
}
