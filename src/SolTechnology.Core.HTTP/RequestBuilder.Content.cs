using System.Net.Http.Headers;
using System.Net.Http.Json;
using SolTechnology.Avro;

namespace SolTechnology.Core.HTTP;

public partial class RequestBuilder
{
    /// <summary>
    /// Materialises the accumulated builder state into a fresh
    /// <see cref="HttpRequestMessage"/>. Called once per terminal verb so the
    /// same builder can be reused across multiple sends.
    /// </summary>
    private HttpRequestMessage BuildRequest(HttpMethod method)
    {
        var request = new HttpRequestMessage(method, _path);

        // Handlers and the error path read the policy from the request; a null policy means defaults.
        if (_policy is not null)
        {
            request.Options.Set(PolicyOptionsKey, _policy);
        }

        // Body first, so content headers below have a target.
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
                // Streams to the network without an intermediate string and sets Content-Type: application/json.
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
}
