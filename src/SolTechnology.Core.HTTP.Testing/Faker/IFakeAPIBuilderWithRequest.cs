 namespace SolTechnology.Core.HTTP.Testing.Faker
{
    public interface IFakeApiBuilderWithRequest<out TApiClient> where TApiClient : class
    {
        /// <summary>
        /// Arranges the request matcher by invoking the fake client method directly. Because the lambda
        /// receives the fake (which implements <typeparamref name="TApiClient"/>), the call is fully
        /// IntelliSense-driven and compile-time type-checked — no reflection, no <c>object?[]</c> args.
        /// </summary>
        /// <example><code>fixture.Fake&lt;IGoogleClient&gt;().WithRequest(x => x.GetLocationOfCity(cityName));</code></example>
        IFakeApiBuilderWithResponse WithRequest(Action<TApiClient> request);
    }
}
