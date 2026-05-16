﻿using SolTechnology.Avro;
using System.Buffers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JetBrains.Annotations;

namespace SolTechnology.Core.HTTP;

public class RequestBuilder
{
    /// <summary>
    /// Per-request options key used to flow the effective
    /// <see cref="HttpPolicyConfiguration"/> through to delegating handlers and
    /// the error-formatting path. Reading this key inside
    /// <see cref="ThrowOnFailureAsync"/> lets <see cref="HttpRequestFailedException"/>
    /// honour the client-scoped <c>IncludeResponseBodyInException</c> flag without
    /// a service-locator on <see cref="HttpClient"/>.
    /// </summary>
    public static readonly HttpRequestOptionsKey<HttpPolicyConfiguration> PolicyOptionsKey =
        new("SolTechnology.Core.HTTP.Policy");

    private readonly HttpClient _httpClient;
    private readonly HttpPolicyConfiguration? _policy;

    public RequestBuilder(HttpClient httpClient, string path)
        : this(httpClient, path, policy: null)
    {
    }

    /// <summary>
    /// Constructs a builder that flows <paramref name="policy"/> through every
    /// materialised <see cref="HttpRequestMessage"/> via
    /// <see cref="HttpRequestMessage.Options"/>. Use the
    /// <see cref="HttpClientExtensions.CreateRequest(HttpClient, string, HttpPolicyConfiguration)"/>
    /// overload from consumer code; this constructor is the implementation seam.
    /// </summary>
    public RequestBuilder(HttpClient httpClient, string path, HttpPolicyConfiguration? policy)
    {
        _httpClient = httpClient;
        _path = path;
        _policy = policy;
    }

    // Cached options instance. JsonSerializerOptions builds an internal metadata
    // cache on first use per (Type, Options) pair; constructing a fresh options
    // object per call defeats that cache and is a documented STJ perf trap.
    //
    // Semantics chosen to minimise breakage vs. the previous Newtonsoft.Json
    // implementation:
    //   - PropertyNamingPolicy = null         → preserve original casing on write
    //                                            (Newtonsoft default; STJ default
    //                                             would camelCase silently).
    //   - PropertyNameCaseInsensitive = true  → tolerate any casing on read
    //                                            (matches Newtonsoft's behaviour).
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
    };

    // Builder state — collected via fluent calls and materialised into a fresh
    // HttpRequestMessage inside each terminal verb. We deliberately do NOT cache
    // a HttpRequestMessage: HttpClient.SendAsync marks a message as "sent" and
    // a second SendAsync on the same instance throws InvalidOperationException
    // ("The request message was already sent."). Re-using the builder for
    // multiple sends is a natural-looking pattern, so we make it safe by
    // building per-call.
    private readonly string _path;
    private readonly List<(string Name, string Value)> _headers = new();
    private object? _body;
    private DataType _bodyType;
    private bool _hasBody;
    private DataType _responseType;
    private JsonSerializerOptions? _jsonOptionsOverride;

    private JsonSerializerOptions JsonOptions => _jsonOptionsOverride ?? DefaultJsonOptions;

    /// <summary>
    /// Replaces the default <see cref="JsonSerializerOptions"/> for this request only.
    /// <para>
    /// Defaults (preserved property casing, case-insensitive read) match the
    /// previous Newtonsoft-compatible behaviour. Override here when a specific
    /// endpoint requires camelCase output, custom converters, polymorphism,
    /// or any other STJ feature gated by <see cref="JsonSerializerOptions"/>.
    /// </para>
    /// </summary>
    public RequestBuilder WithJsonOptions(JsonSerializerOptions options)
    {
        _jsonOptionsOverride = options ?? throw new ArgumentNullException(nameof(options));
        return this;
    }


    public RequestBuilder WithHeader(string name, string value)
    {
        // Defer validation to BuildRequest — at that point we know the verb and
        // whether HttpContent exists, so content-headers (Content-Type, etc.)
        // can be routed correctly without the StreamContent(Stream.Null) hack
        // the old implementation needed.
        _headers.Add((name, value));
        return this;
    }

    public RequestBuilder WithBody(object body, DataType dataType = DataType.Json)
    {
        if (body != null)
        {
            _body = body;
            _bodyType = dataType;
            _hasBody = true;
        }
        return this;
    }

    public RequestBuilder WithResponseType(DataType dataType)
    {
        _responseType = dataType;
        return this;
    }

    /// <summary>
    /// Materialises the accumulated builder state into a fresh
    /// <see cref="HttpRequestMessage"/>. Called once per terminal verb so the
    /// same builder can be reused across multiple sends.
    /// </summary>
    private HttpRequestMessage BuildRequest(HttpMethod method)
    {
        var request = new HttpRequestMessage(method, _path);

        // Flow the effective policy through HttpRequestMessage.Options so
        // downstream handlers (resilience pipeline, error formatter) can read
        // it without a service-locator on HttpClient. Null policy is left
        // unset — readers fall back to defaults.
        if (_policy is not null)
        {
            request.Options.Set(PolicyOptionsKey, _policy);
        }

        // Body first — so content-headers have a real target by the time we
        // start applying WithHeader entries below.
        if (_hasBody && _body is not null)
        {
            request.Content = BuildContent(_body, _bodyType);
        }

        foreach (var (name, value) in _headers)
        {
            if (request.Headers.TryAddWithoutValidation(name, value))
            {
                continue;
            }

            if (request.Content is not null &&
                request.Content.Headers.TryAddWithoutValidation(name, value))
            {
                continue;
            }

            throw new ArgumentException("Invalid header name: " + name, nameof(name));
        }

        return request;
    }

    private HttpContent BuildContent(object body, DataType dataType)
    {
        switch (dataType)
        {
            case DataType.Json:
                // JsonContent streams the payload directly to the network — no
                // intermediate string allocation, no LOH spike on large bodies.
                // It also sets Content-Type: application/json automatically.
                return JsonContent.Create(body, inputType: body.GetType(), mediaType: null, options: JsonOptions);

            case DataType.Avro:
                var avroBytes = AvroConvert.Serialize(body);
                var avroContent = new ByteArrayContent(avroBytes);
                avroContent.Headers.Remove("Content-Type");
                avroContent.Headers.ContentType = new MediaTypeHeaderValue("application/avro");
                return avroContent;

            default:
                throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
        }
    }

    /// <summary>
    /// Sends the request and returns the raw <see cref="HttpResponseMessage"/>.
    /// <para>
    /// <strong>Caller owns disposal</strong> — the response holds an open socket / stream
    /// buffer until disposed. Wrap with <c>using</c>:
    /// <code>using var response = await client.CreateRequest("/x").GetAsync(ct);</code>
    /// </para>
    /// <para>
    /// Marked with <see cref="MustDisposeResourceAttribute"/> so Rider / ReSharper /
    /// JetBrains analysers flag callers that drop the returned message — under load
    /// a missed dispose leaks a socket / connection-pool slot until GC.
    /// </para>
    /// </summary>
    [MustDisposeResource]
    public Task<HttpResponseMessage> GetAsync(CancellationToken cancellationToken = default)
        => SendRaw(HttpMethod.Get, cancellationToken);

    /// <inheritdoc cref="GetAsync(CancellationToken)" />
    [MustDisposeResource]
    public Task<HttpResponseMessage> PostAsync(CancellationToken cancellationToken = default)
        => SendRaw(HttpMethod.Post, cancellationToken);

    /// <inheritdoc cref="GetAsync(CancellationToken)" />
    [MustDisposeResource]
    public Task<HttpResponseMessage> PutAsync(CancellationToken cancellationToken = default)
        => SendRaw(HttpMethod.Put, cancellationToken);

    /// <inheritdoc cref="GetAsync(CancellationToken)" />
    [MustDisposeResource]
    public Task<HttpResponseMessage> PatchAsync(CancellationToken cancellationToken = default)
        => SendRaw(HttpMethod.Patch, cancellationToken);

    /// <inheritdoc cref="GetAsync(CancellationToken)" />
    [MustDisposeResource]
    public Task<HttpResponseMessage> DeleteAsync(CancellationToken cancellationToken = default)
        => SendRaw(HttpMethod.Delete, cancellationToken);

    public Task<TResponse> GetAsync<TResponse>(CancellationToken cancellationToken = default)
        => Send<TResponse>(HttpMethod.Get, cancellationToken);

    public Task<TResponse> PostAsync<TResponse>(CancellationToken cancellationToken = default)
        => Send<TResponse>(HttpMethod.Post, cancellationToken);

    public Task<TResponse> PutAsync<TResponse>(CancellationToken cancellationToken = default)
        => Send<TResponse>(HttpMethod.Put, cancellationToken);

    public Task<TResponse> PatchAsync<TResponse>(CancellationToken cancellationToken = default)
        => Send<TResponse>(HttpMethod.Patch, cancellationToken);

    public Task<TResponse> DeleteAsync<TResponse>(CancellationToken cancellationToken = default)
        => Send<TResponse>(HttpMethod.Delete, cancellationToken);

    private Task<HttpResponseMessage> SendRaw(HttpMethod method, CancellationToken cancellationToken)
    {
        // Caller owns the response (and transitively the request — modern
        // HttpClient/SocketsHttpHandler disposes the request when the response
        // is disposed). We cannot wrap `request` in a `using` here because the
        // response may still need it during body streaming.
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
            await ThrowOnFailureAsync(request, response, cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Builds and throws a <see cref="HttpRequestFailedException"/> carrying the request
    /// method, URI, status, reason phrase, and (best-effort) response body. The body is
    /// captured only when the effective <see cref="HttpPolicyConfiguration.IncludeResponseBodyInException"/>
    /// flag is on — by default it is off so upstream payloads (which may contain PII or
    /// secrets) never reach the exception object and, transitively, the logging pipeline.
    /// </summary>
    private async Task ThrowOnFailureAsync(HttpRequestMessage request, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var policy = ResolvePolicy(request);
        var body = policy.IncludeResponseBodyInException
            ? await TryReadCappedBodyAsync(response, cancellationToken).ConfigureAwait(false)
            : null;

        var uri = request.RequestUri ?? _httpClient.BaseAddress;

        throw new HttpRequestFailedException(
            request.Method,
            uri,
            response.StatusCode,
            response.ReasonPhrase,
            body);
    }

    /// <summary>
    /// Resolves the effective <see cref="HttpPolicyConfiguration"/> for a request.
    /// Order: per-request options (set by <see cref="BuildRequest"/>) → builder-scoped
    /// <see cref="_policy"/> → defaults. A null policy at every layer falls back to
    /// the conservative production defaults defined on the type itself.
    /// </summary>
    private HttpPolicyConfiguration ResolvePolicy(HttpRequestMessage request)
    {
        if (request.Options.TryGetValue(PolicyOptionsKey, out var fromOptions) && fromOptions is not null)
        {
            return fromOptions;
        }

        return _policy ?? new HttpPolicyConfiguration();
    }

    /// <summary>
    /// Reads up to <see cref="MaxCapturedBodyBytes"/> of the response body for inclusion in
    /// <see cref="HttpRequestFailedException.ResponseBody"/>. Bounded read so a 50 MB HTML
    /// error page from a mis-configured upstream cannot stall the caller or pin large
    /// allocations on the LOH while the exception is in flight through logging sinks.
    /// <para>
    /// Uses <see cref="ArrayPool{T}"/> for the staging buffer so a burst of failures
    /// (e.g. circuit-breaker-induced 5xx flood) does not allocate one fresh 8 KiB array
    /// per request. The pool returns same-or-larger arrays — the loop bound is
    /// computed from the requested capacity, not from <c>buffer.Length</c>.
    /// </para>
    /// </summary>
    private static async Task<string?> TryReadCappedBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        // +1 byte sentinel so we can detect that the body was longer than the cap
        // and append a truncation marker — saves callers from chasing phantom bugs
        // caused by silently chopped responses.
        const int capacity = MaxCapturedBodyBytes + 1;
        var buffer = ArrayPool<byte>.Shared.Rent(capacity);
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            var read = 0;
            while (read < capacity)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(read, capacity - read), cancellationToken).ConfigureAwait(false);
                if (n == 0)
                {
                    break;
                }
                read += n;
            }

            if (read == 0)
            {
                return null;
            }

            var truncated = read > MaxCapturedBodyBytes;
            var usable = truncated ? MaxCapturedBodyBytes : read;
            var text = System.Text.Encoding.UTF8.GetString(buffer, 0, usable);

            return truncated ? text + TruncationMarker : text;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Caller cancelled — propagate the cancellation rather than masking it as a
            // server failure. Without this re-throw the caller would see the upstream's
            // HTTP status code instead of the cancellation signal they explicitly raised.
            throw;
        }
        catch
        {
            // Reading the body is best-effort. If the stream is already consumed or the
            // network drops mid-read, we still want to surface the status code + URI so
            // the caller has actionable diagnostics. Swallow silently.
            return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Upper bound on response-body bytes captured in <see cref="HttpRequestFailedException.ResponseBody"/>.
    /// 8 KB is enough for any structured-error JSON payload (RFC 7807 problem details, GraphQL
    /// errors, vendor-specific shapes) while keeping a hostile or accidental 50 MB HTML page
    /// from pinning memory inside the exception object as it flows through logging sinks.
    /// </summary>
    private const int MaxCapturedBodyBytes = 8 * 1024;

    private const string TruncationMarker = "… [response body truncated]";
}
