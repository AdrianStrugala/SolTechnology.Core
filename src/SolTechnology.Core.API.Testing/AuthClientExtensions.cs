using System.Net.Http.Headers;

namespace SolTechnology.Core.API.Testing
{
    /// <summary>
    /// Ergonomic helpers for creating <see cref="HttpClient"/>s against an <see cref="APIFixture{TEntryPoint}"/>'s
    /// in-memory host.
    /// </summary>
    public static class AuthClientExtensions
    {
        extension<TEntryPoint>(APIFixture<TEntryPoint> fixture) where TEntryPoint : class
        {
            /// <summary>
            /// Creates a client whose <c>Authorization</c> header is set to <paramref name="scheme"/> +
            /// <paramref name="token"/>. Scheme-agnostic: pass <c>"Bearer"</c>, a custom test scheme, etc.
            /// </summary>
            public HttpClient CreateAuthorizedClient(string scheme, string? token = null)
            {
                ArgumentNullException.ThrowIfNull(fixture);
                ArgumentException.ThrowIfNullOrWhiteSpace(scheme);

                var client = fixture.TestServer.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, token);
                return client;
            }

            /// <summary>Creates a client with no <c>Authorization</c> header — for anonymous / unauthenticated paths.</summary>
            public HttpClient CreateAnonymousClient()
            {
                ArgumentNullException.ThrowIfNull(fixture);
                return fixture.TestServer.CreateClient();
            }
        }
    }
}

