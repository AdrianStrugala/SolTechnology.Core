using System.Linq.Expressions;
using System.Reflection;
using SolTechnology.Core.Faker.WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace SolTechnology.Core.Faker.FakesBase;

public abstract class FakeService<TApiClient> :
    IFakeServiceBuilderWithRequest<TApiClient>,
    IFakeServiceBuilderWithResponse
    where TApiClient : class

{
    protected IRespondWithAProvider Provider = null!;
    private WireMockServer _mockServer = null!;

    protected abstract string BaseUrl { get; }

    public void Register(WireMockServer mockServer)
    {
        _mockServer = mockServer;
    }

    public IFakeServiceBuilderWithResponse WithRequest(
        Expression<Func<TApiClient, Delegate>> selector,
        object?[]? parameters = null)
    {
        parameters ??= new object[] { };
        var method = GetMethodInfo(selector)!.Name;
        GetType().GetMethod(method)!.Invoke(this, parameters);
        return this;
    }

    public void WithResponse(Action<IResponseBuilder> configure)
    {
        var asJson = (IResponseBuilder builder) => configure(new JsonResponseBuilderDecorator(builder));

        ArgumentNullException.ThrowIfNull(configure);
        var builder = Response.Create();
        asJson.Invoke(builder);
        Provider.RespondWith(builder);
    }

    protected IRespondWithAProvider BuildRequest(IRequestBuilder request)
    {
        return _mockServer.Given(request);
    }

    private static MethodInfo? GetMethodInfo<T>(Expression<Func<T, Delegate>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var unaryExpression = (UnaryExpression)expression.Body;
        var methodCallExpression = (MethodCallExpression)unaryExpression.Operand;
        var methodInfoExpression = (ConstantExpression)methodCallExpression.Object!;
        return methodInfoExpression.Value as MethodInfo;
    }
}