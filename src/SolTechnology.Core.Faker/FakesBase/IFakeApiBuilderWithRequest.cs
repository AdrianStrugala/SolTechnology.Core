using System.Linq.Expressions;

namespace SolTechnology.Core.Faker.FakesBase
{
    public interface IFakeApiBuilderWithRequest<TApiClient> where TApiClient : class
    {
        public IFakeApiBuilderWithResponse WithRequest(
            Expression<Func<TApiClient, Delegate>> selector,
            params object?[]? parameters);
    }
}
