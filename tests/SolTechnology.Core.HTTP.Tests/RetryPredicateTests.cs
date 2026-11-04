using System.Diagnostics.Metrics;
using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Polly;
using SolTechnology.Core.HTTP.Telemetry;

namespace SolTechnology.Core.HTTP.Tests;

/// <summary>
/// Verifies the <see cref="HttpPolicyConfiguration.RetryPredicate"/> opt-in:
/// when configured, it further restricts retries based on response body content.
/// </summary>
public sealed class RetryPredicateTests
{
    private static ResiliencePipeline<HttpResponseMessage> BuildPipeline(
        Func<HttpResponseMessage, ValueTask<bool>>? predicate = null)
    {
        var metrics = new HttpClientMetrics(new DummyMeterFactory());
        var factory = new HttpPolicyFactory(NullLogger<HttpPolicyFactory>.Instance, metrics);
        var cfg = new HttpPolicyConfiguration
        {
            MaxRequestRetries = 2,
            RetryInitialDelay = 1,
            RetryTimeout = 50,
            RequestTimeout = 5_000,
            CircuitBreakerMinimumThroughput = 100, // effectively disable breaker for these tests
            RetryPredicate = predicate
        };

        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();
        factory.Configure(builder, cfg, "test");
        return builder.Build();
    }

    private static HttpResponseMessage TransientWithBody(string json)
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test/")
        };
        return response;
    }

    private sealed class DummyMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);
        public void Dispose() { }
    }

    [Test]
    public async Task NoPredicate_TransientStatus_Retries_Normally()
    {
        var pipeline = BuildPipeline(predicate: null);
        var attempts = 0;

        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(TransientWithBody("""{"recoverable": false}"""));
        });

        // Without predicate, standard behaviour: retries on 500 regardless of body.
        attempts.Should().Be(3); // 1 initial + 2 retries
    }

    [Test]
    public async Task WithRecoverableOnly_AllRecoverable_Retries()
    {
        var pipeline = BuildPipeline(RetryPredicates.RecoverableOnly);
        var attempts = 0;

        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(TransientWithBody("""{"recoverable": true}"""));
        });

        attempts.Should().Be(3); // retries allowed — body says recoverable
    }

    [Test]
    public async Task WithRecoverableOnly_NonRecoverable_StopsRetrying()
    {
        var pipeline = BuildPipeline(RetryPredicates.RecoverableOnly);
        var attempts = 0;

        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(TransientWithBody("""{"recoverable": false}"""));
        });

        attempts.Should().Be(1); // predicate said "don't retry" — single attempt
    }

    [Test]
    public async Task WithRecoverableOnly_NoRecoverableField_Retries()
    {
        // No "recoverable" field — benefit of the doubt, allow retry.
        var pipeline = BuildPipeline(RetryPredicates.RecoverableOnly);
        var attempts = 0;

        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(TransientWithBody("""{"title": "server error"}"""));
        });

        attempts.Should().Be(3);
    }

    [Test]
    public async Task WithRecoverableOnly_EmptyBody_Retries()
    {
        var pipeline = BuildPipeline(RetryPredicates.RecoverableOnly);
        var attempts = 0;

        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("", Encoding.UTF8),
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test/")
            };
            return ValueTask.FromResult(response);
        });

        attempts.Should().Be(3); // empty body — benefit of the doubt
    }

    [Test]
    public async Task RetryPredicate_LargeErrorBody_ReadsAtMost8KiB()
    {
        // Arrange
        var pipeline = BuildPipeline(RetryPredicates.RecoverableOnly);
        var largeBody = Encoding.UTF8.GetBytes("{\"recoverable\": false, \"padding\": \"" + new string('x', 64 * 1024) + "\"}");
        var sources = new List<CountingStream>();
        var attempts = 0;

        // Act
        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            var source = new CountingStream(largeBody);
            sources.Add(source);
            return ValueTask.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StreamContent(source, bufferSize: 1),
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test/")
            });
        });

        // Assert
        attempts.Should().Be(3, "a body over the cap is not inspected, so the transient status decides");
        sources.Should().AllSatisfy(source => source.BytesRead.Should().BeLessThan(9 * 1024, "buffering stops at the 8 KiB cap plus one copy chunk"));
        sources[^1].BytesRead.Should().Be(0, "the last attempt's response is returned to the caller unread");
    }

    [TestCase("[1, 2]")]
    [TestCase("\"plain text\"")]
    [TestCase("not json {{{")]
    public async Task RecoverableOnly_BodyIsNotAJsonObject_AllowsRetry(string body)
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        // Act
        var shouldRetry = await RetryPredicates.RecoverableOnly(response);

        // Assert
        shouldRetry.Should().BeTrue();
    }

    private sealed class CountingStream(byte[] data) : Stream
    {
        private readonly MemoryStream _inner = new(data, writable: false);

        public long BytesRead { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Test]
    public async Task WithRecoverableOnly_NonRetryableStatus_StillNotRetried()
    {
        // A 400 Bad Request is NOT in the retryable status set — the predicate
        // cannot EXPAND retries, only restrict. Even with recoverable=true in body.
        var pipeline = BuildPipeline(RetryPredicates.RecoverableOnly);
        var attempts = 0;

        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""{"recoverable": true}""", Encoding.UTF8, "application/json"),
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test/")
            };
            return ValueTask.FromResult(response);
        });

        attempts.Should().Be(1); // 400 is not transient — never retried regardless of body
    }
}

