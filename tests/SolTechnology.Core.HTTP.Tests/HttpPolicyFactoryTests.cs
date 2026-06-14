using System;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using SolTechnology.Core.HTTP;
using SolTechnology.Core.HTTP.Telemetry;
using NUnit.Framework;

namespace SolTechnology.Core.HTTP.Tests;

/// <summary>
/// Verifies the Polly v8 resilience pipeline produced by
/// <see cref="HttpPolicyFactory"/>: retry on transient status codes, circuit
/// breaker after sustained failures, Retry-After honouring, idempotent-only
/// retry, and the optional outer request budget.
/// </summary>
public sealed class HttpPolicyFactoryTests
{
    private static ResiliencePipeline<HttpResponseMessage> BuildPipeline(
        Action<HttpPolicyConfiguration>? tune = null,
        string clientName = "test-client")
    {
        var metrics = new HttpClientMetrics(new DummyMeterFactory());
        var factory = new HttpPolicyFactory(NullLogger<HttpPolicyFactory>.Instance, metrics);
        var cfg = new HttpPolicyConfiguration
        {
            // Tighten defaults so tests stay sub-second. Production defaults
            // (10 s timeouts) would make the suite painfully slow without
            // contributing any signal.
            MaxRequestRetries = 3,
            RetryInitialDelay = 10,
            RetryTimeout = 200,
            RequestTimeout = 5_000,
            CircuitBreakerMinimumThroughput = 4,
            CircuitBreakerSamplingDuration = 5_000,
            CircuitBreakerDelayDuration = 5_000,
            CircuitBreakerFailureThreshold = 0.5,
            // Default behaviour: idempotent-only. Per-test override via tune().
        };
        tune?.Invoke(cfg);

        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();
        factory.Configure(builder, cfg, clientName);
        return builder.Build();
    }

    private static HttpResponseMessage Ok() => new(HttpStatusCode.OK);

    /// <summary>
    /// Polly's <see cref="OutcomeArgs{TResult,TArgs}.Outcome"/> reads
    /// <c>RequestMessage</c> off the response — without attaching one, the
    /// idempotent-verb check has no method to inspect. The helper attaches a
    /// <see cref="HttpRequestMessage"/> so the predicate behaves like in
    /// production.
    /// </summary>
    private static HttpResponseMessage Status(HttpStatusCode code, HttpMethod? method = null)
    {
        var response = new HttpResponseMessage(code);
        if (method is not null)
        {
            response.RequestMessage = new HttpRequestMessage(method, "http://test/");
        }
        return response;
    }

    private sealed class DummyMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);
        public void Dispose() { }
    }

    [Test]
    public async Task Retry_TransientFailureThenSuccess_SucceedsOnLaterAttempt()
    {
        var pipeline = BuildPipeline();
        var attempts = 0;

        var result = await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(attempts < 3 ? Status(HttpStatusCode.ServiceUnavailable, HttpMethod.Get) : Ok());
        });

        attempts.Should().Be(3);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Retry_NonTransientStatus_DoesNotRetry()
    {
        var pipeline = BuildPipeline();
        var attempts = 0;

        var result = await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Status(HttpStatusCode.BadRequest, HttpMethod.Get));
        });

        attempts.Should().Be(1);
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    [TestCase(HttpStatusCode.RequestTimeout)]        // 408
    [TestCase(HttpStatusCode.TooManyRequests)]       // 429
    [TestCase(HttpStatusCode.InternalServerError)]   // 500
    [TestCase(HttpStatusCode.BadGateway)]            // 502
    [TestCase(HttpStatusCode.ServiceUnavailable)]    // 503
    [TestCase(HttpStatusCode.GatewayTimeout)]        // 504
    public async Task Retry_TransientStatusCodes_AreRetriedForGet(HttpStatusCode code)
    {
        var pipeline = BuildPipeline();
        var attempts = 0;

        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Status(code, HttpMethod.Get));
        });

        attempts.Should().Be(4); // initial + 3 retries
    }

    [Test]
    public async Task Retry_PostByDefault_DoesNotRetry()
    {
        // Default RetryOnUnsafeVerbs=false. A POST that 5xx's must NOT be
        // retried — replaying it can duplicate side effects upstream
        // (booking, payment, email send). The single attempt surfaces the
        // failure to the caller, who decides what to do.
        var pipeline = BuildPipeline();
        var attempts = 0;

        var result = await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Status(HttpStatusCode.InternalServerError, HttpMethod.Post));
        });

        attempts.Should().Be(1);
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task Retry_PatchByDefault_DoesNotRetry()
    {
        var pipeline = BuildPipeline();
        var attempts = 0;

        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Status(HttpStatusCode.ServiceUnavailable, HttpMethod.Patch));
        });

        attempts.Should().Be(1);
    }

    [Test]
    public async Task Retry_PostWithUnsafeVerbsEnabled_Retries()
    {
        // Caller opted in: endpoint is documented as idempotent (e.g. uses
        // Idempotency-Key) or has its own dedup. Retry is honoured.
        var pipeline = BuildPipeline(cfg => cfg.RetryOnUnsafeVerbs = true);
        var attempts = 0;

        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Status(HttpStatusCode.ServiceUnavailable, HttpMethod.Post));
        });

        attempts.Should().Be(4);
    }

    [Test]
    public async Task Retry_PutAndDelete_AreRetriedAsIdempotentVerbs()
    {
        // RFC 7231: PUT and DELETE are idempotent — retrying them after a 5xx
        // is safe. They must be retried by default, no opt-in needed. Use a
        // fresh pipeline per verb so the breaker's failure window does not
        // bleed across the two retry sequences.
        var putAttempts = 0;
        await BuildPipeline().ExecuteAsync(_ =>
        {
            putAttempts++;
            return ValueTask.FromResult(Status(HttpStatusCode.BadGateway, HttpMethod.Put));
        });

        var deleteAttempts = 0;
        await BuildPipeline().ExecuteAsync(_ =>
        {
            deleteAttempts++;
            return ValueTask.FromResult(Status(HttpStatusCode.BadGateway, HttpMethod.Delete));
        });

        putAttempts.Should().Be(4);
        deleteAttempts.Should().Be(4);
    }

    [Test]
    public async Task RetryAfter_DeltaSeconds_DelaysRequestedAmount()
    {
        var pipeline = BuildPipeline();
        var attempts = 0;
        var firstAttemptAt = DateTimeOffset.MinValue;
        var secondAttemptAt = DateTimeOffset.MinValue;

        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            if (attempts == 1) firstAttemptAt = DateTimeOffset.UtcNow;
            if (attempts == 2) secondAttemptAt = DateTimeOffset.UtcNow;

            if (attempts == 1)
            {
                var resp = Status(HttpStatusCode.TooManyRequests, HttpMethod.Get);
                resp.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(200));
                return ValueTask.FromResult(resp);
            }

            return ValueTask.FromResult(Ok());
        });

        attempts.Should().Be(2);
        var observed = secondAttemptAt - firstAttemptAt;
        observed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(150));
    }

    [Test]
    public async Task RetryAfter_ExcessiveValue_CappedAtRetryTimeout()
    {
        var pipeline = BuildPipeline();
        var attempts = 0;
        var first = DateTimeOffset.MinValue;
        var second = DateTimeOffset.MinValue;

        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            if (attempts == 1) first = DateTimeOffset.UtcNow;
            if (attempts == 2) second = DateTimeOffset.UtcNow;

            if (attempts == 1)
            {
                var resp = Status(HttpStatusCode.TooManyRequests, HttpMethod.Get);
                resp.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(60));
                return ValueTask.FromResult(resp);
            }
            return ValueTask.FromResult(Ok());
        });

        attempts.Should().Be(2);
        (second - first).Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task UsePollyFalse_ProducesPassthroughPipeline()
    {
        var pipeline = BuildPipeline(cfg => cfg.UsePolly = false);
        var attempts = 0;

        var result = await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Status(HttpStatusCode.InternalServerError, HttpMethod.Get));
        });

        attempts.Should().Be(1);
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task OverallRequestBudget_TerminatesRetrySequenceWhenExceeded()
    {
        // Outer budget caps the whole logical call. With aggressive retries
        // and a 250 ms budget, the inner pipeline must yield before completing
        // all 3 retries — the pipeline raises a TimeoutRejectedException
        // (Polly's signal that the outer strategy cancelled the operation).
        var pipeline = BuildPipeline(cfg =>
        {
            cfg.OverallRequestBudget = 250;
            cfg.RetryInitialDelay = 100;
            cfg.RetryTimeout = 1_000;
            cfg.MaxRequestRetries = 10;
        });

        var attempts = 0;
        Func<Task> act = async () => await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(Status(HttpStatusCode.ServiceUnavailable, HttpMethod.Get));
        });

        await act.Should().ThrowAsync<Polly.Timeout.TimeoutRejectedException>();
        // We never reach the 10-retry ceiling because the outer budget fires first.
        attempts.Should().BeLessThan(10);
    }
}

