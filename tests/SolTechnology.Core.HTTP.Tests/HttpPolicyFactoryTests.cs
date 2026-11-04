using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.CircuitBreaker;
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
        string clientName = "test-client",
        ILogger<HttpPolicyFactory>? logger = null,
        HttpClientMetrics? metrics = null)
    {
        metrics ??= new HttpClientMetrics(new DummyMeterFactory());
        var factory = new HttpPolicyFactory(logger ?? NullLogger<HttpPolicyFactory>.Instance, metrics);
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

    [TestCase("POST", 1)]
    [TestCase("PATCH", 1)]
    [TestCase("GET", 4)]
    [TestCase("PUT", 4)]
    public async Task Retry_UnsafeVerb_HttpRequestException_IsNotRetried(string method, int expectedAttempts)
    {
        // Arrange
        var pipeline = BuildPipeline();
        var attempts = 0;

        // Act
        var act = () => ExecuteWithRequest(pipeline, new HttpMethod(method), _ =>
        {
            attempts++;
            throw new HttpRequestException("connection reset");
        });

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
        attempts.Should().Be(expectedAttempts);
    }

    [TestCase("POST", 1)]
    [TestCase("PATCH", 1)]
    [TestCase("GET", 4)]
    [TestCase("PUT", 4)]
    public async Task Retry_UnsafeVerb_Timeout_IsNotRetried(string method, int expectedAttempts)
    {
        // Arrange
        var pipeline = BuildPipeline(cfg => cfg.RequestTimeout = 20);
        var attempts = 0;

        // Act
        var act = () => ExecuteWithRequest(pipeline, new HttpMethod(method), async token =>
        {
            attempts++;
            await Task.Delay(Timeout.Infinite, token);
            return Ok();
        });

        // Assert
        await act.Should().ThrowAsync<Polly.Timeout.TimeoutRejectedException>();
        attempts.Should().Be(expectedAttempts);
    }

    [TestCase("POST")]
    [TestCase("PATCH")]
    public async Task Retry_UnsafeVerb_WithRetryOnUnsafeVerbsEnabled_IsRetried(string method)
    {
        // Arrange
        var pipeline = BuildPipeline(cfg => cfg.RetryOnUnsafeVerbs = true);
        var attempts = 0;

        // Act
        var act = () => ExecuteWithRequest(pipeline, new HttpMethod(method), _ =>
        {
            attempts++;
            throw new HttpRequestException("connection reset");
        });

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
        attempts.Should().Be(4);
    }

    private static async Task<HttpResponseMessage> ExecuteWithRequest(
        ResiliencePipeline<HttpResponseMessage> pipeline,
        HttpMethod method,
        Func<CancellationToken, ValueTask<HttpResponseMessage>> callback,
        CancellationToken cancellationToken = default)
    {
        var context = ResilienceContextPool.Shared.Get(cancellationToken);
        using var request = new HttpRequestMessage(method, "http://test/");
        context.SetRequestMessage(request);
        try
        {
            return await pipeline.ExecuteAsync(ctx => callback(ctx.CancellationToken), context);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
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

    [Test]
    public async Task Configure_ZeroRetries_SendsOnce_AndKeepsCircuitBreaker()
    {
        // Arrange
        var pipeline = BuildPipeline(cfg => cfg.MaxRequestRetries = 0);
        var attempts = 0;

        // Act
        for (var call = 0; call < 4; call++)
        {
            await ExecuteWithRequest(pipeline, HttpMethod.Get, _ =>
            {
                attempts++;
                return ValueTask.FromResult(Status(HttpStatusCode.ServiceUnavailable, HttpMethod.Get));
            });
        }
        var afterBreak = () => ExecuteWithRequest(pipeline, HttpMethod.Get, _ => ValueTask.FromResult(Ok()));

        // Assert
        attempts.Should().Be(4, "zero retries means one attempt per call");
        await afterBreak.Should().ThrowAsync<BrokenCircuitException>();
    }

    [Test]
    public async Task CallerCancellation_Burst_DoesNotOpenCircuit_NorRecordRetries()
    {
        // Arrange
        var logger = new TemplateCapturingLogger();
        using var metrics = new MetricCapture();
        var pipeline = BuildPipeline(logger: CreateLogger(logger), metrics: metrics.Metrics);
        var attempts = 0;

        // Act
        for (var call = 0; call < 8; call++)
        {
            using var caller = new CancellationTokenSource();
            var cancelled = () => ExecuteWithRequest(pipeline, HttpMethod.Get, _ =>
            {
                attempts++;
                caller.Cancel();
                throw new TaskCanceledException("caller gave up", null, caller.Token);
            }, caller.Token);
            await cancelled.Should().ThrowAsync<OperationCanceledException>();
        }
        using var next = await ExecuteWithRequest(pipeline, HttpMethod.Get, _ => ValueTask.FromResult(Ok()));

        // Assert
        attempts.Should().Be(8);
        next.StatusCode.Should().Be(HttpStatusCode.OK, "caller cancellations are not upstream failures");
        logger.Entries.Should().NotContain(entry => entry.Template != null && entry.Template.StartsWith("Retry policy activated"));
        metrics.Retries.Should().BeEmpty();
    }

    [Test]
    public async Task HttpClientTimeout_TaskCanceledWithoutCallerCancellation_IsRetried()
    {
        // Arrange
        var pipeline = BuildPipeline();
        var attempts = 0;

        // Act
        var act = () => ExecuteWithRequest(pipeline, HttpMethod.Get, _ =>
        {
            attempts++;
            throw new TaskCanceledException("HttpClient.Timeout elapsed");
        });

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
        attempts.Should().Be(4);
    }

    [Test]
    public async Task CircuitBreaker_Opened_LogsClientNameWithExceptionAsLogException()
    {
        // Arrange
        var logger = new TemplateCapturingLogger();
        var pipeline = BuildPipeline(logger: CreateLogger(logger));
        var failure = new HttpRequestException("connection reset");

        // Act
        for (var call = 0; call < 4; call++)
        {
            var act = () => ExecuteWithRequest(pipeline, HttpMethod.Post, _ => throw failure);
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        // Assert
        var opened = logger.Entries.Should().ContainSingle(entry => entry.Template != null && entry.Template.StartsWith("Circuit breaker opened")).Which;
        opened.Level.Should().Be(LogLevel.Warning);
        opened.Template.Should().Be("Circuit breaker opened for [{ClientName}]");
        opened.Message.Should().Be("Circuit breaker opened for [test-client]");
        opened.Exception.Should().BeSameAs(failure);
    }

    [TestCase("GET")]
    [TestCase("PUT")]
    public async Task Retry_AfterException_TagsRetryMetricWithRequestMethod(string method)
    {
        // Arrange
        using var metrics = new MetricCapture();
        var pipeline = BuildPipeline(metrics: metrics.Metrics);

        // Act
        var act = () => ExecuteWithRequest(pipeline, new HttpMethod(method), _ => throw new HttpRequestException("connection reset"));
        await act.Should().ThrowAsync<HttpRequestException>();

        // Assert
        metrics.Retries.Should().HaveCount(3);
        metrics.Retries.Should().OnlyContain(tags => (string?)tags["http.method"] == method && (string?)tags["outcome"] == "exception");
    }

    private static ILogger<HttpPolicyFactory> CreateLogger(TemplateCapturingLogger capture)
        => new Logger<HttpPolicyFactory>(new LoggerFactory([capture]));

    private sealed class MetricCapture : IDisposable
    {
        private readonly MeterListener _listener = new();

        public MetricCapture()
        {
            Metrics = new HttpClientMetrics(new DummyMeterFactory());
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(instrument, Metrics.Retries))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
            {
                var copy = new Dictionary<string, object?>();
                foreach (var tag in tags)
                {
                    copy[tag.Key] = tag.Value;
                }
                lock (Retries)
                {
                    Retries.Add(copy);
                }
            });
            _listener.Start();
        }

        public HttpClientMetrics Metrics { get; }

        public List<Dictionary<string, object?>> Retries { get; } = [];

        public void Dispose() => _listener.Dispose();
    }
}
