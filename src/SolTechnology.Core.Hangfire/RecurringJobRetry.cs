namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Retry budget of recurring jobs: set explicitly so Hangfire's global default of 10 never applies.
/// </summary>
internal static class RecurringJobRetry
{
    public const int RecoverableResultAttempts = 3;
}
