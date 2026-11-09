using System.Text;
using System.Text.Json;
using JsonConverter.Abstractions;
using WireMock;
using WireMock.ResponseBuilders;

namespace SolTechnology.Core.HTTP.Testing.WireMock;

public partial class JsonResponseBuilderDecorator
{
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
}
