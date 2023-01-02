using System.Linq.Expressions;
using System.Reflection;
using TaleCode.Faker.WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace TaleCode.Faker.FakesBase;

public abstract class FakeService<TApiClient> :
    IFakeServiceBuilderWithRequest<TApiClient>,
    IFakeServiceBuilderWithResponse
    where TApiClient : class

{
    private IWireMockFakerConfigurator<TApiClient>? _buildConfiguration;
    private IRespondWithAProvider _provider = null!;

    protected abstract IWireMockFakerConfigurator<TApiClient> Configure(WireMockServer mockServer);

    public void Register(WireMockServer mockServer) =>
        _buildConfiguration = Configure(mockServer) ??
                              throw new InvalidOperationException($"{nameof(Configure)} should never return null");

    public IFakeServiceBuilderWithResponse WithRequest(Expression<Func<TApiClient, Delegate>> selector,
        int priority = 10, Action<IRequestBuilder>? configure = null)
    {
        var method = GetMethodInfo(selector)!.Name;
        var requestInfo = (RequestInfo)GetType().GetMethod(method)!.Invoke(this, new object[] { })!;

        _provider = _buildConfiguration!.BuildRequest(requestInfo, configure).AtPriority(priority);
        return this;
    }

    public void WithResponse(Action<IResponseBuilder> configure)
    {
        var asJson = (IResponseBuilder builder) => configure(new JsonResponseBuilderDecorator(builder));

        ArgumentNullException.ThrowIfNull(configure);
        var builder = Response.Create();
        asJson.Invoke(builder);
        _provider.RespondWith(builder);
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