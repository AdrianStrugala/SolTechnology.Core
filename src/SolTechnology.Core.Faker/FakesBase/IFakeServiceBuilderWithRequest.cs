using System.Linq.Expressions;

namespace SolTechnology.Core.Faker.FakesBase
{
    public interface IFakeServiceBuilderWithRequest<TApiClient> where TApiClient : class
    {
        public IFakeServiceBuilderWithResponse WithRequest(
            Expression<Func<TApiClient, Delegate>> selector,
            params object?[]? parameters);
    }
}
