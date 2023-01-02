using System.Linq.Expressions;
using WireMock.RequestBuilders;

namespace TaleCode.Faker.FakesBase
{
    public interface IFakeServiceBuilderWithRequest<TApiClient> where TApiClient : class
    {
        public IFakeServiceBuilderWithResponse WithRequest(Expression<Func<TApiClient, Delegate>> selector,
            int priority = 10, Action<IRequestBuilder>? configure = null);
    }
}
