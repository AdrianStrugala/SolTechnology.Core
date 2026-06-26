using System.ComponentModel.DataAnnotations;

namespace SolTechnology.Core.HTTP;

public class HttpPolicyConfiguration
{
    /// <summary>
    /// Configuration section that supplies a global policy default for every
    /// registered HTTP client (overridden per client via
    /// <c>HTTPClients:{name}:Policy</c>).
    /// </summary>
    public const string SectionName = "HttpPolicy";

    // All durations are in milliseconds.
    //
    // Defaults are deliberately conservative-but-realistic: aggressive enough to
    // recover quickly from transient upstream blips, slow enough not to amplify
    // outages with a retry storm. They aim to be safe out-of-the-box for the
    // majority of REST/gRPC-over-HTTP backends. Tune per-client via
    // "HTTPClients:{name}:Policy" or globally via "HttpPolicy" in appsettings.
    public bool UsePolly { get; set; } = true;

    // Cap on a single retry backoff delay.
    [Range(1, int.MaxValue, ErrorMessage = nameof(RetryTimeout) + " must be greater than zero.")]
    public int RetryTimeout { get; set; } = 30_000;

    // Base delay for the jitter sequence. 200 ms is the conventional "polite
    // client" baseline that lets the upstream recover before we hammer it again.
    [Range(0, int.MaxValue, ErrorMessage = nameof(RetryInitialDelay) + " must be non-negative.")]
    public int RetryInitialDelay { get; set; } = 200;

    // 2 retries (= 3 attempts total) keeps the worst-case wall-clock under
    // RequestTimeout * 3 even with full backoff. The previous 3 made a single
    // failed request take up to 4 attempts × RequestTimeout, which under a
    // brownout exhausted the HttpClient connection pool before the breaker
    // tripped. Override per-client where higher tolerance is justified.
    [Range(0, 100, ErrorMessage = nameof(MaxRequestRetries) + " must be between 0 and 100.")]
    public int MaxRequestRetries { get; set; } = 2;

    /// <summary>
    /// Whether non-idempotent verbs (<c>POST</c>, <c>PATCH</c>, <c>CONNECT</c>)
    /// are eligible for automatic retry. Defaults to <c>false</c> — replaying a
    /// <c>POST</c> after a network-induced 5xx / timeout can double-create
    /// resources upstream (duplicate bookings, duplicate charges).
    /// <para>
    /// Enable per-client only when the upstream endpoint is explicitly
    /// documented as idempotent (e.g. via an <c>Idempotency-Key</c> header) or
    /// the caller has its own deduplication.
    /// </para>
    /// </summary>
    public bool RetryOnUnsafeVerbs { get; set; } = false;

    /// <summary>
    /// Whether <see cref="HttpRequestFailedException.ResponseBody"/> is populated.
    /// Defaults to <c>false</c> — upstream response bodies may contain PII,
    /// secrets, or partner-confidential payloads, and every standard exception
    /// formatter (Serilog, App Insights, Sentry) serialises the full exception
    /// state into log sinks.
    /// <para>
    /// Enable per-client (or per-environment) only for diagnostic builds where
    /// the operator accepts the disclosure risk in exchange for richer error
    /// context.
    /// </para>
    /// </summary>
    public bool IncludeResponseBodyInException { get; set; } = false;

    /// <summary>
    /// Optional predicate consulted <b>after</b> the standard transient-status + verb checks pass.
    /// When set, the retry pipeline calls it with the response and retries <b>only</b> if it returns
    /// <c>true</c>. Use this to inspect the response body and refuse retries when the upstream
    /// signals a non-recoverable business error (e.g. <c>Error.Recoverable = false</c> in a
    /// ProblemDetails envelope).
    /// <para>
    /// <b>Restrict-only semantics:</b> a <c>false</c> return short-circuits the retry even when the
    /// status code is normally retryable. A <c>true</c> return does <b>not</b> make a non-retryable
    /// status retryable — it only confirms "yes, retry this transient-looking failure".
    /// </para>
    /// <para>
    /// <b>Body buffering:</b> the response is buffered (via <c>LoadIntoBufferAsync</c>) before the
    /// predicate is called, so reading <c>Content</c> does not corrupt the stream for the caller.
    /// </para>
    /// <para>
    /// Default: <c>null</c> (no body inspection — existing behaviour unchanged).
    /// </para>
    /// </summary>
    public Func<HttpResponseMessage, ValueTask<bool>>? RetryPredicate { get; set; }

    /// <summary>
    /// Optional outer time budget (milliseconds) for the entire resilience
    /// pipeline — retries, breaker state changes, and per-attempt timeout
    /// combined. When set, requests that exceed this budget are cancelled even
    /// if retries are still pending, preventing a single logical call from
    /// pinning a thread / connection-pool slot for tens of seconds during an
    /// upstream brownout.
    /// <para>
    /// Null disables the outer budget (per-attempt <see cref="RequestTimeout"/>
    /// remains active). When set, it must be greater than
    /// <see cref="RequestTimeout"/> to allow at least one full attempt.
    /// </para>
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = nameof(OverallRequestBudget) + " must be greater than zero when set.")]
    public int? OverallRequestBudget { get; set; }

    // Polly's CircuitBreakerStrategyOptions.FailureRatio accepts 0..1 inclusive.
    // Out-of-range values fail deep inside the pipeline at first use with an
    // unclear exception; we surface them eagerly at options validation time.
    [Range(0.0, 1.0, ErrorMessage = nameof(CircuitBreakerFailureThreshold) + " must be a ratio between 0.0 and 1.0.")]
    public double CircuitBreakerFailureThreshold { get; set; } = 0.3;

    [Range(1, int.MaxValue, ErrorMessage = nameof(CircuitBreakerDelayDuration) + " must be greater than zero.")]
    public int CircuitBreakerDelayDuration { get; set; } = 10_000;

    // 10 s sampling window matches the lower MinimumThroughput below — together
    // they make the breaker meaningful for low-traffic clients (most internal
    // service-to-service traffic) without requiring per-client tuning.
    [Range(1, int.MaxValue, ErrorMessage = nameof(CircuitBreakerSamplingDuration) + " must be greater than zero.")]
    public int CircuitBreakerSamplingDuration { get; set; } = 10_000;

    // 5 is the smallest value that lets the breaker compute a stable failure
    // ratio (Polly enforces a floor of 2). The previous 10 effectively disabled
    // the breaker for clients under ~1 req/s per instance.
    [Range(2, int.MaxValue, ErrorMessage = nameof(CircuitBreakerMinimumThroughput) + " must be at least 2 (Polly requirement).")]
    public int CircuitBreakerMinimumThroughput { get; set; } = 5;

    // 10 s per-attempt timeout is tight enough to fail fast on a hung upstream
    // and small enough that (MaxRequestRetries + 1) attempts × RequestTimeout
    // stays under typical caller deadlines (30 s). Override per-client for
    // legitimately slow endpoints (large search, file processing).
    [Range(1, int.MaxValue, ErrorMessage = nameof(RequestTimeout) + " must be greater than zero.")]
    public int RequestTimeout { get; set; } = 10_000;
}


