using System.Text.RegularExpressions;
using SolTechnology.Core.Faker.FakesBase;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.Server;
using IRequestBuilder = WireMock.RequestBuilders.IRequestBuilder;

namespace SolTechnology.Core.Faker.WireMock;

public class WireMockFakerConfigurator<TClient> : IWireMockFakerConfigurator<TClient>
    where TClient : class
{
    private readonly Regex _parameterRegex = new("{(.*?)}");

    private readonly WireMockServer _server;

    private string? _baseUrl;

    public WireMockFakerConfigurator(WireMockServer server) => _server = server;

    public IWireMockFakerConfigurator<TClient> WithBaseUrl(string url)
    {
        _baseUrl = url;
        return this;
    }

    public IRespondWithAProvider BuildRequest(
        RequestInfo requestInfo,
        Dictionary<string, string> pathParameters,
        Dictionary<string, string> queryParameters,
        Action<IRequestBuilder>? configure = null)
    {
        var path = requestInfo.Path;
        var httpMethod = requestInfo.HttpMethod;

        foreach (var parameter in pathParameters)
        {
            path = path.Replace($"{{{parameter.Key}}}", parameter.Value);
        }

        var request = Request
            .Create()
            .UsingMethod(httpMethod.Method)
            .WithPath(new WildcardMatcher($"/{_baseUrl}/{_parameterRegex.Replace(path, "*").Trim('/')}"));

        foreach (var queryParameter in requestInfo.QueryParameters)
        {
            request.WithParam(queryParameter.Key, queryParameter.Value);
        }
        foreach (var queryParameter in queryParameters)
        {
            request.WithParam(queryParameter.Key, queryParameter.Value);
        }

        configure?.Invoke(request);
        return _server.Given(request);
    }
}
