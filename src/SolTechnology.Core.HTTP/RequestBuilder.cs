using System.Text.Json;

namespace SolTechnology.Core.HTTP;

/// <summary>
/// Fluent builder that sends one request shape through an <see cref="HttpClient"/>. Start it with
/// <see cref="HttpClientExtensions.CreateRequest(HttpClient, string)"/>; a builder can send more than once.
/// </summary>
public partial class RequestBuilder
{
    /// <summary>
    /// Per-request options key that carries the <see cref="HttpPolicyConfiguration"/> passed to
    /// <see cref="HttpClientExtensions.CreateRequest(HttpClient, string, HttpPolicyConfiguration)"/>.
    /// <see cref="ThrowOnFailure"/> reads <c>IncludeResponseBodyInException</c> from it; the
    /// named client's policy registered by <c>AddSolHTTPClient</c> does not reach it, so the flag
    /// applies only to requests created with that overload.
    /// </summary>
    public static readonly HttpRequestOptionsKey<HttpPolicyConfiguration> PolicyOptionsKey =
        new("SolTechnology.Core.HTTP.Policy");

    // One cached instance: a fresh JsonSerializerOptions per call defeats System.Text.Json's metadata cache.
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly HttpPolicyConfiguration? _policy;

    // State only: each send builds a fresh HttpRequestMessage, because HttpClient refuses to send one twice.
    private readonly string _path;
    private readonly List<(string Name, string Value)> _headers = new();
    private object? _body;
    private DataType _bodyType;
    private bool _hasBody;
    private DataType _responseType;
    private JsonSerializerOptions? _jsonOptionsOverride;

    /// <summary>
    /// Constructs a builder for <paramref name="path"/>, relative to the client's base address.
    /// </summary>
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

    /// <summary>
    /// Adds a request header; content headers such as <c>Content-Type</c> are applied to the body.
    /// </summary>
    public RequestBuilder WithHeader(string name, string value)
    {
        _headers.Add((name, value));
        return this;
    }

    /// <summary>
    /// Sets the request body, serialised as <paramref name="dataType"/>. A <c>null</c> body is ignored.
    /// </summary>
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

    /// <summary>
    /// Sets the format the typed verbs deserialise the response from. Default: <see cref="DataType.Json"/>.
    /// </summary>
    public RequestBuilder WithResponseType(DataType dataType)
    {
        _responseType = dataType;
        return this;
    }
}
