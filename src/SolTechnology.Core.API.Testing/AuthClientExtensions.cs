using System.Net.Http.Headers;

namespace SolTechnology.Core.API.Testing
{
    /// <summary>
    /// Ergonomic helpers for creating <see cref="HttpClient"/>s against an <see cref="APIFixture{TEntryPoint}"/>'s
    /// in-memory host. Generalised from the per-app auth-client helpers seen in production suites
    /// (e.g. MTS <c>GetUserAuthClient</c> / <c>GetNoAuthClient</c>) so apps stop hand-rolling them.
    /// </summary>
    public static class AuthClientExtensions
    {
        /// <summary>
        /// Creates a client whose <c>Authorization</c> header is set to <paramref name="scheme"/> +
        /// <paramref name="token"/>. Scheme-agnostic: pass <c>"Bearer"</c>, a custom test scheme, etc.
        /// </summary>
        public static HttpClient CreateAuthorizedClient<TEntryPoint>(
            this APIFixture<TEntryPoint> fixture,
            string scheme,
            string? token = null)
            where TEntryPoint : class
        {
            ArgumentNullException.ThrowIfNull(fixture);
            ArgumentException.ThrowIfNullOrWhiteSpace(scheme);

            var client = fixture.TestServer.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, token);
            return client;
        }

        /// <summary>Creates a client with no <c>Authorization</c> header — for anonymous / unauthenticated paths.</summary>
        public static HttpClient CreateAnonymousClient<TEntryPoint>(this APIFixture<TEntryPoint> fixture)
            where TEntryPoint : class
        {
            ArgumentNullException.ThrowIfNull(fixture);
            return fixture.TestServer.CreateClient();
        }
    }
}

