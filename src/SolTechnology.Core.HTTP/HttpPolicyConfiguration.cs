using System.ComponentModel.DataAnnotations;

namespace SolTechnology.Core.HTTP;

/// <summary>
/// Resilience policy of an HTTP client: retry, circuit breaker and timeouts. All durations are in
/// milliseconds. The defaults are conservative for typical REST backends; tune them per client via
/// <c>HTTPClients:{name}:Policy</c> or globally via <c>HttpPolicy</c>.
/// </summary>
public class HttpPolicyConfiguration
{
    /// <summary>
    /// Configuration section that supplies a global policy default for every
    /// registered HTTP client (overridden per client via
    /// <c>HTTPClients:{name}:Policy</c>).
    /// </summary>
    public const string SectionName = "HttpPolicy";

    /// <summary>
    /// Whether the retry, circuit-breaker and timeout pipeline runs. Default <c>true</c>; <c>false</c>
    /// logs a warning at start and leaves <see cref="HTTPClientConfiguration.TimeoutSeconds"/> as the only deadline.
    /// </summary>
    public bool UsePolly { get; set; } = true;

    /// <summary>
    /// Cap on a single retry backoff delay and on an honoured <c>Retry-After</c>. Default 30 000.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = nameof(RetryTimeout) + " must be greater than zero.")]
    public int RetryTimeout { get; set; } = 30_000;

    /// <summary>
    /// Base delay of the exponential, jittered backoff. Default 200.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = nameof(RetryInitialDelay) + " must be non-negative.")]
    public int RetryInitialDelay { get; set; } = 200;

    /// <summary>
    /// Retries after the first attempt. Default 2 (3 attempts): the worst case is 3 ×
    /// <see cref="RequestTimeout"/> plus two backoff delays, unless <see cref="OverallRequestBudget"/> is lower.
    /// </summary>
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
    /// Honoured per request only: pass a policy with this flag set to
    /// <c>CreateRequest(path, policy)</c>. A named client's policy from
    /// <c>AddSolHTTPClient</c> or configuration does not reach the request.
    /// Enable it only for diagnostic calls where the operator accepts the
    /// disclosure risk in exchange for richer error context.
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
    /// <b>Body buffering:</b> the response body is buffered (at most 8 KiB, with the request's
    /// cancellation token) before the predicate is called, so reading <c>Content</c> does not
    /// corrupt the stream for the caller. A larger body skips the predicate, and so does the last
    /// attempt, whose response is returned to the caller either way.
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

    /// <summary>
    /// Failure ratio (0.0–1.0) over <see cref="CircuitBreakerSamplingDuration"/> that opens the
    /// circuit. Default 0.3; validated at start because Polly rejects other values only at first use.
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = nameof(CircuitBreakerFailureThreshold) + " must be a ratio between 0.0 and 1.0.")]
    public double CircuitBreakerFailureThreshold { get; set; } = 0.3;

    /// <summary>
    /// How long the circuit stays open before a half-open probe. Default 10 000.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = nameof(CircuitBreakerDelayDuration) + " must be greater than zero.")]
    public int CircuitBreakerDelayDuration { get; set; } = 10_000;

    /// <summary>
    /// Window over which the failure ratio is computed. Default 10 000.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = nameof(CircuitBreakerSamplingDuration) + " must be greater than zero.")]
    public int CircuitBreakerSamplingDuration { get; set; } = 10_000;

    /// <summary>
    /// Calls needed in the sampling window before the circuit can open. Default 5; Polly's floor is 2.
    /// </summary>
    [Range(2, int.MaxValue, ErrorMessage = nameof(CircuitBreakerMinimumThroughput) + " must be at least 2 (Polly requirement).")]
    public int CircuitBreakerMinimumThroughput { get; set; } = 5;

    /// <summary>
    /// Per-attempt timeout. Default 10 000; raise it per client for slow endpoints.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = nameof(RequestTimeout) + " must be greater than zero.")]
    public int RequestTimeout { get; set; } = 10_000;
}


