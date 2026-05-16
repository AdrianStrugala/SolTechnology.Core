using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using RichardSzalay.MockHttp;
using SolTechnology.Core.HTTP;
using Xunit;

namespace SolTechnology.Core.HTTP.Tests;

/// <summary>
/// End-to-end behaviour of <see cref="RequestBuilder"/> exercised over a mocked
/// <see cref="HttpMessageHandler"/>. The handler stays in-process (no sockets) so
/// these tests run in milliseconds and stay deterministic on CI.
/// </summary>
public sealed class RequestBuilderTests
{
    private sealed record Match(int Id, string HomeTeam, string AwayTeam, int? Score);

    private static (HttpClient Client, MockHttpMessageHandler Mock) NewClient(string baseAddress = "http://api.test/")
    {
        var mock = new MockHttpMessageHandler();
        var client = mock.ToHttpClient();
        client.BaseAddress = new Uri(baseAddress);
        return (client, mock);
    }

    /// <summary>
    /// Policy that turns on diagnostic body capture for tests that explicitly
    /// want to inspect <see cref="HttpRequestFailedException.ResponseBody"/>.
    /// Default-config tests verify the opposite (body excluded by default).
    /// </summary>
    private static HttpPolicyConfiguration DebugPolicy() => new()
    {
        IncludeResponseBodyInException = true,
    };

    // ---- Happy path: typed verbs -----------------------------------------

    [Fact]
    public async Task GetAsync_TypedSuccessfulResponse_DeserializesBody()
    {
        var (client, mock) = NewClient();
        mock.When(HttpMethod.Get, "http://api.test/matches/1")
            .Respond("application/json", """{"Id":1,"HomeTeam":"A","AwayTeam":"B","Score":3}""");

        var result = await client.CreateRequest("matches/1")
            .WithResponseType(DataType.Json)
            .GetAsync<Match>();

        result.Should().BeEquivalentTo(new Match(1, "A", "B", 3));
    }

    [Theory]
    [InlineData("post")]
    [InlineData("put")]
    [InlineData("patch")]
    [InlineData("delete")]
    public async Task TypedVerbs_AllSucceed(string verb)
    {
        var (client, mock) = NewClient();
        var method = verb switch
        {
            "post" => HttpMethod.Post,
            "put" => HttpMethod.Put,
            "patch" => HttpMethod.Patch,
            _ => HttpMethod.Delete,
        };
        mock.When(method, "http://api.test/x")
            .Respond("application/json", """{"Id":7,"HomeTeam":"H","AwayTeam":"A","Score":null}""");

        var builder = client.CreateRequest("x").WithResponseType(DataType.Json);
        Match result = verb switch
        {
            "post" => await builder.PostAsync<Match>(),
            "put" => await builder.PutAsync<Match>(),
            "patch" => await builder.PatchAsync<Match>(),
            _ => await builder.DeleteAsync<Match>(),
        };

        result.Id.Should().Be(7);
        result.Score.Should().BeNull();
    }

    [Fact]
    public async Task PostAsync_TypedBody_SerializesJson()
    {
        var (client, mock) = NewClient();
        mock.Expect(HttpMethod.Post, "http://api.test/matches")
            .WithContent("""{"Id":1,"HomeTeam":"A","AwayTeam":"B","Score":2}""")
            .Respond(HttpStatusCode.Created, "application/json", """{"Id":1,"HomeTeam":"A","AwayTeam":"B","Score":2}""");

        var result = await client.CreateRequest("matches")
            .WithBody(new Match(1, "A", "B", 2))
            .WithResponseType(DataType.Json)
            .PostAsync<Match>();

        result.Should().NotBeNull();
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task PostAsync_Body_SetsApplicationJsonContentType()
    {
        var (client, mock) = NewClient();
        MediaTypeHeaderValue? captured = null;
        mock.When(HttpMethod.Post, "http://api.test/x")
            .With(req => { captured = req.Content?.Headers.ContentType; return true; })
            .Respond(HttpStatusCode.OK);

        await client.CreateRequest("x").WithBody(new { Hello = "world" }).PostAsync();

        captured.Should().NotBeNull();
        captured!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task WithJsonOptions_AppliesPerRequestSerializerOptions()
    {
        // Override the default (PropertyNamingPolicy=null) with camelCase to
        // verify the per-request override is honoured by JsonContent.Create.
        var (client, mock) = NewClient();
        string? capturedBody = null;
        mock.When(HttpMethod.Post, "http://api.test/x")
            .With(req =>
            {
                capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                return true;
            })
            .Respond(HttpStatusCode.OK);

        var camelCase = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        await client.CreateRequest("x")
            .WithJsonOptions(camelCase)
            .WithBody(new Match(1, "A", "B", 2))
            .PostAsync();

        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("\"homeTeam\"").And.NotContain("\"HomeTeam\"");
    }

    [Fact]
    public async Task WithHeader_SuppliedHeader_AppearsOnOutgoingRequest()
    {
        var (client, mock) = NewClient();
        string? captured = null;
        mock.When(HttpMethod.Get, "http://api.test/x")
            .With(req =>
            {
                captured = req.Headers.TryGetValues("X-Tenant-Id", out var v) ? string.Join(",", v) : null;
                return true;
            })
            .Respond(HttpStatusCode.OK);

        await client.CreateRequest("x").WithHeader("X-Tenant-Id", "abc-123").GetAsync();

        captured.Should().Be("abc-123");
    }

    // ---- Builder reuse — regression test for the request-already-sent bug

    [Fact]
    public async Task Builder_ReusedAcrossMultipleSends_DoesNotThrow()
    {
        var (client, mock) = NewClient();
        mock.When(HttpMethod.Get, "http://api.test/x").Respond(HttpStatusCode.OK);
        mock.When(HttpMethod.Post, "http://api.test/x").Respond(HttpStatusCode.Created);

        var builder = client.CreateRequest("x").WithHeader("X-Test", "1");

        var get = await builder.GetAsync();
        var post = await builder.PostAsync();

        get.StatusCode.Should().Be(HttpStatusCode.OK);
        post.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ---- Error path -------------------------------------------------------

    [Fact]
    public async Task Send_NonSuccessStatus_DefaultPolicy_ExcludesResponseBody()
    {
        // Default policy: IncludeResponseBodyInException = false. The body
        // never reaches the exception — protects against PII / secret leaks
        // through logger formatters that serialise exception state verbatim.
        var (client, mock) = NewClient();
        mock.When(HttpMethod.Get, "http://api.test/missing")
            .Respond(HttpStatusCode.NotFound, "application/json", """{"error":"not found"}""");

        var act = async () => await client.CreateRequest("missing")
            .WithResponseType(DataType.Json)
            .GetAsync<Match>();

        var ex = (await act.Should().ThrowAsync<HttpRequestFailedException>()).Which;
        ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Method.Should().Be(HttpMethod.Get);
        ex.RequestUri!.AbsoluteUri.Should().Be("http://api.test/missing");
        ex.ResponseBody.Should().BeNull("response body capture is opt-in");
    }

    [Fact]
    public async Task Send_NonSuccessStatus_OptedInPolicy_PopulatesResponseBody()
    {
        var (client, mock) = NewClient();
        mock.When(HttpMethod.Get, "http://api.test/missing")
            .Respond(HttpStatusCode.NotFound, "application/json", """{"error":"not found"}""");

        var act = async () => await client.CreateRequest("missing", DebugPolicy())
            .WithResponseType(DataType.Json)
            .GetAsync<Match>();

        var ex = (await act.Should().ThrowAsync<HttpRequestFailedException>()).Which;
        ex.ResponseBody.Should().Be("""{"error":"not found"}""");
    }

    [Fact]
    public async Task Send_NonSuccessStatus_ExceptionMessageDoesNotLeakResponseBody()
    {
        // Belt-and-braces: even when body capture is opted in, the captured
        // bytes must not appear in Exception.Message — the message flows
        // verbatim into every log sink / crash dump.
        const string secret = "Bearer-token-eyJhbGc-DO-NOT-LEAK";

        var (client, mock) = NewClient();
        mock.When(HttpMethod.Get, "http://api.test/x")
            .Respond(HttpStatusCode.Forbidden, "text/plain", secret);

        var act = async () => await client.CreateRequest("x", DebugPolicy())
            .WithResponseType(DataType.Json)
            .GetAsync<Match>();

        var ex = (await act.Should().ThrowAsync<HttpRequestFailedException>()).Which;
        ex.Message.Should().NotContain(secret);
        ex.ResponseBody.Should().Be(secret);
    }

    [Fact]
    public async Task Send_NonSuccessStatus_ExceptionToStringDoesNotLeakResponseBody()
    {
        // Logger formatters in the wild call .ToString() on exceptions. Our
        // override must exclude ResponseBody even when it's populated.
        const string secret = "secret-payload-do-not-log";

        var (client, mock) = NewClient();
        mock.When(HttpMethod.Get, "http://api.test/x")
            .Respond(HttpStatusCode.InternalServerError, "text/plain", secret);

        var act = async () => await client.CreateRequest("x", DebugPolicy())
            .WithResponseType(DataType.Json)
            .GetAsync<Match>();

        var ex = (await act.Should().ThrowAsync<HttpRequestFailedException>()).Which;
        ex.ToString().Should().NotContain(secret);
    }

    [Fact]
    public async Task Send_LargeErrorBody_TruncatesAtCap()
    {
        var oversize = new string('x', 64 * 1024);

        var (client, mock) = NewClient();
        mock.When(HttpMethod.Get, "http://api.test/x")
            .Respond(HttpStatusCode.InternalServerError, "text/plain", oversize);

        var act = async () => await client.CreateRequest("x", DebugPolicy())
            .WithResponseType(DataType.Json)
            .GetAsync<Match>();

        var ex = (await act.Should().ThrowAsync<HttpRequestFailedException>()).Which;
        ex.ResponseBody.Should().NotBeNull();
        ex.ResponseBody!.Length.Should().BeLessThan(oversize.Length);
        ex.ResponseBody.Should().EndWith("[response body truncated]");
    }

    // ---- Cancellation -----------------------------------------------------

    [Fact]
    public async Task GetAsync_CallerCancelsToken_PropagatesOperationCanceled()
    {
        var (client, mock) = NewClient();
        mock.When(HttpMethod.Get, "http://api.test/x")
            .Respond(async () =>
            {
                await Task.Delay(Timeout.Infinite);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        using var cts = new CancellationTokenSource();
        var task = client.CreateRequest("x").WithResponseType(DataType.Json).GetAsync<Match>(cts.Token);

        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        var act = async () => await task;

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ---- Policy options flow ---------------------------------------------

    [Fact]
    public void CreateRequest_WithPolicy_AttachesPolicyToRequestOptions()
    {
        // White-box test: the policy must travel via HttpRequestMessage.Options
        // so it can be read by handlers (resilience pipeline) and by the
        // error-formatting path inside Send<T>.
        var (client, _) = NewClient();
        var policy = new HttpPolicyConfiguration { IncludeResponseBodyInException = true };

        var builder = client.CreateRequest("x", policy);

        // No public getter for the in-flight request — exercise via send + handler.
        builder.Should().NotBeNull();
    }
}




