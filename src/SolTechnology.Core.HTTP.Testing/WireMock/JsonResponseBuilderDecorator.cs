using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using JsonConverter.Abstractions;
using Microsoft.AspNetCore.Http;
using WireMock;
using WireMock.Models;
using WireMock.ResponseBuilders;
using WireMock.Settings;
using WireMock.Types;
using WireMock.WebSockets;

namespace SolTechnology.Core.HTTP.Testing.WireMock;

/// <summary>
/// Wraps WireMock's <see cref="IResponseBuilder"/> so that <c>WithBodyAsJson(object)</c> serializes with
/// <c>System.Text.Json</c>; every other member forwards to the wrapped builder and returns this decorator.
/// </summary>
public class JsonResponseBuilderDecorator : IResponseBuilder
{
    private readonly IResponseBuilder _originalBuilder;

    /// <summary>Wraps <paramref name="originalBuilder"/>.</summary>
    public JsonResponseBuilderDecorator(IResponseBuilder originalBuilder) => _originalBuilder = originalBuilder;

    /// <inheritdoc />
    public IMapping Mapping => _originalBuilder.Mapping;

    /// <inheritdoc />
    public IResponseMessage ResponseMessage => _originalBuilder.ResponseMessage;

    /// <inheritdoc />
    public Task<(IResponseMessage Message, IMapping? Mapping)> ProvideResponseAsync(IMapping mapping,
        HttpContext context, IRequestMessage requestMessage, WireMockServerSettings settings) =>
        _originalBuilder.ProvideResponseAsync(mapping, context, requestMessage, settings);

    /// <inheritdoc />
    public IResponseBuilder WithCallback(Func<IRequestMessage, IResponseMessage> callbackHandler)
    {
        _originalBuilder.WithCallback(callbackHandler);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithCallback(Func<IRequestMessage, Task<IResponseMessage>> callbackHandler)
    {
        _originalBuilder.WithCallback(callbackHandler);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithWebSocket(Action<IWebSocketBuilder> configure)
    {
        _originalBuilder.WithWebSocket(configure);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithWebSocketProxy(string targetUrl)
    {
        _originalBuilder.WithWebSocketProxy(targetUrl);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithWebSocketProxy(ProxyAndRecordSettings settings)
    {
        _originalBuilder.WithWebSocketProxy(settings);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithSseBody(Func<IRequestMessage, IBlockingQueue<string?>, Task> bodyFactory,
        TimeSpan? timeout = null)
    {
        _originalBuilder.WithSseBody(bodyFactory, timeout);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithDelay(TimeSpan delay)
    {
        _originalBuilder.WithDelay(delay);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithDelay(int milliseconds)
    {
        _originalBuilder.WithDelay(milliseconds);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithRandomDelay(int minimumMilliseconds = 0, int maximumMilliseconds = 60000)
    {
        _originalBuilder.WithRandomDelay(minimumMilliseconds, maximumMilliseconds);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithTransformer(bool transformContentFromBodyAsFile)
    {
        _originalBuilder.WithTransformer(transformContentFromBodyAsFile);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithTransformer(ReplaceNodeOptions options)
    {
        _originalBuilder.WithTransformer(options);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithTransformer(TransformerType transformerType = TransformerType.Handlebars,
        bool transformContentFromBodyAsFile = false, ReplaceNodeOptions options = ReplaceNodeOptions.EvaluateAndTryToConvert)
    {
        _originalBuilder.WithTransformer(transformerType, transformContentFromBodyAsFile, options);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithFault(FaultType faultType, double? percentage = null)
    {
        _originalBuilder.WithFault(faultType, percentage);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithBody(string body, string? destination = "SameAsSource", Encoding? encoding = null)
    {
        _originalBuilder.WithBody(body, destination, encoding);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithBody(string body, string? destination, Encoding? encoding, IJsonConverter? jsonConverter,
        JsonConverterOptions? options = null)
    {
        _originalBuilder.WithBody(body, destination, encoding, jsonConverter, options);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithBody(Func<IRequestMessage, string> bodyFactory, string? destination = "SameAsSource",
        Encoding? encoding = null)
    {
        _originalBuilder.WithBody(bodyFactory, destination);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithBody(Func<IRequestMessage, Task<string>> bodyFactory,
        string? destination = "SameAsSource", Encoding? encoding = null)
    {
        _originalBuilder.WithBody(bodyFactory, destination, encoding);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithBody(byte[] body, string? destination = "SameAsSource", Encoding? encoding = null)
    {
        _originalBuilder.WithBody(body, destination, encoding);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithBody(object body, IJsonConverter converter, JsonConverterOptions? options = null)
    {
        _originalBuilder.WithBody(body, converter, options);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithBody(object body, Encoding? encoding, IJsonConverter converter,
        JsonConverterOptions? options = null)
    {
        _originalBuilder.WithBody(body, encoding, converter, options);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithBodyAsProtoBuf(string protoDefinition, string messageType, object value,
        IJsonConverter? jsonConverter = null, JsonConverterOptions? options = null)
    {
        _originalBuilder.WithBodyAsProtoBuf(protoDefinition, messageType, value, jsonConverter, options);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithBodyAsProtoBuf(IReadOnlyList<string> protoDefinitions, string messageType, object value,
        IJsonConverter? jsonConverter = null, JsonConverterOptions? options = null)
    {
        _originalBuilder.WithBodyAsProtoBuf(protoDefinitions, messageType, value, jsonConverter, options);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithBodyAsProtoBuf(string messageType, object value, IJsonConverter? jsonConverter = null,
        JsonConverterOptions? options = null)
    {
        _originalBuilder.WithBodyAsProtoBuf(messageType, value, jsonConverter, options);
        return this;
    }

    /// <summary>Serializes <paramref name="body"/> with <c>System.Text.Json</c> and sets it as the response body.</summary>
    /// <param name="body">The object to serialize.</param>
    /// <param name="encoding">Not used.</param>
    /// <param name="indented">Writes indented JSON when <see langword="true"/>.</param>
    public IResponseBuilder WithBodyAsJson(object body, Encoding? encoding = null, bool? indented = null)
    {
        var json = JsonSerializer.Serialize(body,
            new JsonSerializerOptions { WriteIndented = indented.GetValueOrDefault() });
        _originalBuilder.WithBody(json);
        return this;
    }

    /// <summary>Serializes <paramref name="body"/> with <c>System.Text.Json</c>, indented when <paramref name="indented"/> is set.</summary>
    public IResponseBuilder WithBodyAsJson(object body, bool indented)
    {
        WithBodyAsJson(body, null, indented);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithBodyAsJson(Func<IRequestMessage, object> bodyFactory, Encoding? encoding = null)
    {
        _originalBuilder.WithBodyAsJson(bodyFactory, encoding);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithBodyAsJson(Func<IRequestMessage, Task<object>> bodyFactory, Encoding? encoding = null)
    {
        _originalBuilder.WithBodyAsJson(bodyFactory, encoding);
        return this;
    }

    /// <inheritdoc />
    public IResponseBuilder WithBodyFromFile(string filename, bool cache = true)
    {
        _originalBuilder.WithBodyFromFile(filename, cache);
        return this;
    }

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
