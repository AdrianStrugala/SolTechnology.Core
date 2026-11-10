using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// Level escalation and tail-based sampling of Chronicle reports. The level is decided from the
/// root alone, before a report exists, so a disabled level never builds one.
/// </summary>
internal sealed class ChronicleEmissionPolicy(ChronicleOptions options, Func<double>? nextSample = null)
{
    private readonly Func<double> _nextSample = nextSample ?? Random.Shared.NextDouble;

    internal LogLevel LevelOf(ChronicleStepStatus status, TimeSpan duration, int? httpStatusCode = null)
    {
        if (status == ChronicleStepStatus.Failed || httpStatusCode >= 500)
        {
            return LogLevel.Error;
        }

        return status == ChronicleStepStatus.Cancelled || httpStatusCode >= 400 || duration >= options.SlowThreshold
            ? LogLevel.Warning
            : LogLevel.Information;
    }

    internal bool ShouldEmit(ChronicleReport report, LogLevel level)
        => report.Status != ChronicleStepStatus.Ok
           || level >= LogLevel.Warning
           || report.IsNPlusOneSuspected
           || report.HasFailedSteps
           || _nextSample() < options.SampleRate;
}
