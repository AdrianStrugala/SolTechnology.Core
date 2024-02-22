using Newtonsoft.Json;
using SolTechnology.Avro;
using System.Net.Http.Headers;

namespace SolTechnology.Core.ApiClient;

public class RequestBuilder
{
    private readonly HttpClient _httpClient;
    private readonly HttpRequestMessage _request;
    private DataType _responseType;

    public RequestBuilder(HttpClient httpClient, string path)
    {
        _httpClient = httpClient;
        _request = new HttpRequestMessage(HttpMethod.Get, path);
    }


    public RequestBuilder WithHeader(string name, string value)
    {
        if (!_request.Headers.TryAddWithoutValidation(name, value))
        {
            if (_request.Content == null)
            {
                _request.Content = new StreamContent(Stream.Null);
            }
            if (!_request.Content.Headers.TryAddWithoutValidation(name, value))
            {
                throw new ArgumentException("Invalid header name: " + name, nameof(name));
            }
        }
        return this;
    }

    public RequestBuilder WithBody(object body, DataType dataType = DataType.Json)
    {
        if (body != null)
        {
            HttpContent httpContent;
            switch (dataType)
            {
                case DataType.Json:
                    var contentJson = JsonConvert.SerializeObject(body);

                    httpContent = new StringContent(contentJson);
                    httpContent.Headers.Remove("Content-Type");
                    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    _request.Content = httpContent;
                    break;

                case DataType.Avro:
                    var avroContent = AvroConvert.Serialize(body);

                    httpContent = new ByteArrayContent(avroContent);
                    httpContent.Headers.Remove("Content-Type");
                    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/avro");

                    _request.Content = httpContent;
                    break;
            }
        }
        return this;
    }

    public RequestBuilder WithResponseType(DataType dataType)
    {
        _responseType = dataType;
        return this;
    }

    public Task<HttpResponseMessage> GetAsync()
    {
        _request.Method = HttpMethod.Get;
        return _httpClient.SendAsync(_request);
    }

    public Task<HttpResponseMessage> PostAsync()
    {
        _request.Method = HttpMethod.Post;
        return _httpClient.SendAsync(_request);
    }

    public Task<HttpResponseMessage> PutAsync()
    {
        _request.Method = HttpMethod.Put;
        return _httpClient.SendAsync(_request);
    }

    public Task<HttpResponseMessage> PatchAsync()
    {
        _request.Method = HttpMethod.Patch;
        return _httpClient.SendAsync(_request);
    }

    public Task<HttpResponseMessage> DeleteAsync()
    {
        _request.Method = HttpMethod.Delete;
        return _httpClient.SendAsync(_request);
    }

    public Task<TResponse> GetAsync<TResponse>()
    {
        _request.Method = HttpMethod.Get;
        return Send<TResponse>();
    }

    public Task<TResponse> PostAsync<TResponse>()
    {
        _request.Method = HttpMethod.Post;
        return Send<TResponse>();
    }

    public Task<TResponse> PutAsync<TResponse>()
    {
        _request.Method = HttpMethod.Put;
        return Send<TResponse>();
    }

    public Task<TResponse> PatchAsync<TResponse>()
    {
        _request.Method = HttpMethod.Patch;
        return Send<TResponse>();
    }

    public Task<TResponse> DeleteAsync<TResponse>()
    {
        _request.Method = HttpMethod.Delete;
        return Send<TResponse>();
    }

    private async Task<TResponse> Send<TResponse>()
    {
        var response = await _httpClient.SendAsync(_request);

        if (response.IsSuccessStatusCode == false)
        {
            HandleErrors(response);
        }

        switch (_responseType)
        {
            case DataType.Json:
                var responseJsonContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TResponse>(responseJsonContent);

            case DataType.Avro:
                var responseAvroContent = await response.Content.ReadAsByteArrayAsync();
                return AvroConvert.Deserialize<TResponse>(responseAvroContent);

            default:
                return default;
        }
    }

    private static void HandleErrors(HttpResponseMessage httpResponseMessage)
    {
        throw new Exception(httpResponseMessage.ReasonPhrase);
    }
}