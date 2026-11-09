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
/// <param name="originalBuilder">The WireMock builder every call is forwarded to.</param>
public partial class JsonResponseBuilderDecorator(IResponseBuilder originalBuilder) : IResponseBuilder
{
    private readonly IResponseBuilder _originalBuilder = originalBuilder;

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
}
