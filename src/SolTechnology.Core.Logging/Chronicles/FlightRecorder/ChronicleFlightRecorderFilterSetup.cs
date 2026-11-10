using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Logging.Chronicles.FlightRecorder;

/// <summary>
/// Default filter rules for the flight-recorder provider, added only when no rule targets its
/// alias or type: the minimum level for every category and Warning for <c>Microsoft</c> and
/// <c>System</c> when enabled, otherwise <see cref="LogLevel.None"/> so MEL never consults it.
/// Post-configure, so it sees the rules from configuration and <c>AddFilter</c>.
/// </summary>
internal sealed class ChronicleFlightRecorderFilterSetup(IOptions<ChronicleOptions> options) : IPostConfigureOptions<LoggerFilterOptions>
{
    private static readonly string ProviderType = typeof(ChronicleFlightRecorderProvider).FullName!;

    public void PostConfigure(string? name, LoggerFilterOptions filter)
    {
        if (filter.Rules.Any(r => r.ProviderName == ChronicleFlightRecorderProvider.Alias || r.ProviderName == ProviderType))
        {
            return;
        }

        var recorder = ChronicleFlightRecorderProvider.ReadOrDisabled(options);
        if (!recorder.Enabled)
        {
            filter.Rules.Add(Rule(null, LogLevel.None));
            return;
        }

        filter.Rules.Add(Rule(null, recorder.MinimumLevel));
        filter.Rules.Add(Rule("Microsoft", LogLevel.Warning));
        filter.Rules.Add(Rule("System", LogLevel.Warning));
    }

    private static LoggerFilterRule Rule(string? category, LogLevel level)
        => new(ChronicleFlightRecorderProvider.Alias, category, level, filter: null);
}
