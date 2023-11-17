using System.Net;
using System.Text;
using System.Text.Json;
using JsonConverter.Abstractions;
using WireMock;
using WireMock.ResponseBuilders;
using WireMock.Settings;
using WireMock.Types;

namespace SolTechnology.Core.Faker.WireMock;

public class JsonResponseBuilderDecorator : IResponseBuilder
{
    private readonly IResponseBuilder _originalBuilder;

    public JsonResponseBuilderDecorator(IResponseBuilder originalBuilder) => _originalBuilder = originalBuilder;

    public Task<(IResponseMessage Message, IMapping? Mapping)> ProvideResponseAsync(IMapping mapping,
        IRequestMessage requestMessage, WireMockServerSettings settings) =>
        _originalBuilder.ProvideResponseAsync(mapping, requestMessage, settings);

    public IResponseBuilder WithCallback(Func<IRequestMessage, ResponseMessage> callbackHandler)
    {
        _originalBuilder.WithCallback(callbackHandler);
        return this;
    }

    public IResponseBuilder WithCallback(Func<IRequestMessage, Task<ResponseMessage>> callbackHandler)
    {
        _originalBuilder.WithCallback(callbackHandler);
        return this;
    }

    public IResponseBuilder WithDelay(TimeSpan delay)
    {
        _originalBuilder.WithDelay(delay);
        return this;
    }

    public IResponseBuilder WithDelay(int milliseconds)
    {
        _originalBuilder.WithDelay(milliseconds);
        return this;
    }

    public IResponseBuilder WithRandomDelay(int minimumMilliseconds = 0, int maximumMilliseconds = 60000)
    {
        _originalBuilder.WithRandomDelay(minimumMilliseconds, maximumMilliseconds);
        return this;
    }

    public IResponseBuilder WithTransformer(bool transformContentFromBodyAsFile)
    {
        _originalBuilder.WithTransformer(transformContentFromBodyAsFile);
        return this;
    }

    public IResponseBuilder WithTransformer(ReplaceNodeOptions options)
    {
        _originalBuilder.WithTransformer(options);
        return this;
    }

    public IResponseBuilder WithTransformer(TransformerType transformerType = TransformerType.Handlebars,
        bool transformContentFromBodyAsFile = false, ReplaceNodeOptions options = ReplaceNodeOptions.None)
    {
        _originalBuilder.WithTransformer(transformerType, transformContentFromBodyAsFile, options);
        return this;
    }

    public IResponseBuilder WithFault(FaultType faultType, double? percentage = null)
    {
        _originalBuilder.WithFault(faultType, percentage);
        return this;
    }

    public IResponseBuilder WithBody(string body, string? destination = "SameAsSource", Encoding? encoding = null)
    {
        _originalBuilder.WithBody(body, destination, encoding);
        return this;
    }

    public IResponseBuilder WithBody(Func<IRequestMessage, string> bodyFactory, string? destination = "SameAsSource",
        Encoding? encoding = null)
    {
        _originalBuilder.WithBody(bodyFactory, destination);
        return this;
    }

    public IResponseBuilder WithBody(Func<IRequestMessage, Task<string>> bodyFactory,
        string? destination = "SameAsSource", Encoding? encoding = null)
    {
        _originalBuilder.WithBody(bodyFactory, destination, encoding);
        return this;
    }

    public IResponseBuilder WithBody(byte[] body, string? destination = "SameAsSource", Encoding? encoding = null)
    {
        _originalBuilder.WithBody(body, destination, encoding);
        return this;
    }

    public IResponseBuilder WithBody(object body, IJsonConverter converter, JsonConverterOptions? options = null)
    {
        _originalBuilder.WithBody(body, converter, options);
        return this;
    }

    public IResponseBuilder WithBody(object body, Encoding? encoding, IJsonConverter converter,
        JsonConverterOptions? options = null)
    {
        _originalBuilder.WithBody(body, encoding, converter, options);
        return this;
    }

    public IResponseBuilder WithBodyAsJson(object body, Encoding? encoding = null, bool? indented = null)
    {
        var json = JsonSerializer.Serialize(body,
            new JsonSerializerOptions {WriteIndented = indented.GetValueOrDefault()});
        _originalBuilder.WithBody(json);
        return this;
    }

    public IResponseBuilder WithBodyAsJson(object body, bool indented)
    {
        WithBodyAsJson(body, null, indented);
        return this;
    }

    public IResponseBuilder WithBodyFromFile(string filename, bool cache = true)
    {
        _originalBuilder.WithBodyFromFile(filename, cache);
        return this;
    }

    public IResponseBuilder WithHeader(string name, params string[] values)
    {
        _originalBuilder.WithHeader(name, values);
        return this;
    }

    public IResponseBuilder WithHeaders(IDictionary<string, string> headers)
    {
        _originalBuilder.WithHeaders(headers);
        return this;
    }

    public IResponseBuilder WithHeaders(IDictionary<string, string[]> headers)
    {
        _originalBuilder.WithHeaders(headers);
        return this;
    }

    public IResponseBuilder WithHeaders(IDictionary<string, WireMockList<string>> headers)
    {
        _originalBuilder.WithHeaders(headers);
        return this;
    }

    public IResponseBuilder WithStatusCode(int code)
    {
        _originalBuilder.WithStatusCode(code);
        return this;
    }

    public IResponseBuilder WithStatusCode(string code)
    {
        _originalBuilder.WithStatusCode(code);
        return this;
    }

    public IResponseBuilder WithStatusCode(HttpStatusCode code)
    {
        _originalBuilder.WithStatusCode(code);
        return this;
    }

    public IResponseBuilder WithSuccess()
    {
        _originalBuilder.WithSuccess();
        return this;
    }

    public IResponseBuilder WithNotFound()
    {
        _originalBuilder.WithNotFound();
        return this;
    }

    public IResponseBuilder WithProxy(string proxyUrl, string? clientX509Certificate2ThumbprintOrSubjectName = null)
    {
        _originalBuilder.WithProxy(proxyUrl, clientX509Certificate2ThumbprintOrSubjectName);
        return this;
    }

    public IResponseBuilder WithProxy(ProxyAndRecordSettings settings)
    {
        _originalBuilder.WithProxy(settings);
        return this;
    }
}
