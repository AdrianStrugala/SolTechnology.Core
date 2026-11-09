using System.Net;
using System.Security.Cryptography.X509Certificates;
using WireMock.ResponseBuilders;
using WireMock.Settings;
using WireMock.Types;

namespace SolTechnology.Core.HTTP.Testing.WireMock;

public partial class JsonResponseBuilderDecorator
{
    /// <inheritdoc />
    public IResponseBuilder WithHeader(string name, params string[] values)
    {
        _originalBuilder.WithHeader(name, values);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithHeaders(IDictionary<string, string> headers)
    {
        _originalBuilder.WithHeaders(headers);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithHeaders(IDictionary<string, string[]> headers)
    {
        _originalBuilder.WithHeaders(headers);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithHeaders(IDictionary<string, WireMockList<string>> headers)
    {
        _originalBuilder.WithHeaders(headers);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithTrailingHeader(string name, params string[] values)
    {
        _originalBuilder.WithTrailingHeader(name, values);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithTrailingHeaders(IDictionary<string, string> headers)
    {
        _originalBuilder.WithTrailingHeaders(headers);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithTrailingHeaders(IDictionary<string, string[]> headers)
    {
        _originalBuilder.WithTrailingHeaders(headers);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithTrailingHeaders(IDictionary<string, WireMockList<string>> headers)
    {
        _originalBuilder.WithTrailingHeaders(headers);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithStatusCode(int code)
    {
        _originalBuilder.WithStatusCode(code);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithStatusCode(string code)
    {
        _originalBuilder.WithStatusCode(code);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithStatusCode(HttpStatusCode code)
    {
        _originalBuilder.WithStatusCode(code);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithSuccess()
    {
        _originalBuilder.WithSuccess();
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithNotFound()
    {
        _originalBuilder.WithNotFound();
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithProxy(string proxyUrl, string? clientX509Certificate2ThumbprintOrSubjectName = null)
    {
        _originalBuilder.WithProxy(proxyUrl, clientX509Certificate2ThumbprintOrSubjectName);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithProxy(ProxyAndRecordSettings settings)
    {
        _originalBuilder.WithProxy(settings);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithProxy(string proxyUrl, X509Certificate2 certificate)
    {
        _originalBuilder.WithProxy(proxyUrl, certificate);
        return this;
    }
}
