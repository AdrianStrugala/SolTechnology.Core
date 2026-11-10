using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Logging.Chronicles.FlightRecorder;

/// <summary>
/// MEL provider that buffers Debug/Trace entries written inside an open Chronicle root. Depends
/// only on the options and the ambient root, never on <see cref="IChronicle"/> or a logger, so the
/// logger factory cannot form a DI cycle with the reporter.
/// </summary>
[ProviderAlias(Alias)]
internal sealed class ChronicleFlightRecorderProvider(IOptions<ChronicleOptions> options) : ILoggerProvider
{
    internal const string Alias = "ChronicleFlightRecorder";

    private static readonly ChronicleFlightRecorderOptions Disabled = new();

    private ChronicleFlightRecorderOptions? _options;

    internal ChronicleFlightRecorderOptions Options => _options ??= ReadOrDisabled(options);

    public ILogger CreateLogger(string categoryName) => new ChronicleFlightRecorderLogger(categoryName, this);

    public void Dispose()
    {
    }

    /// <summary>
    /// Invalid options read as a disabled recorder: the logger factory is built before
    /// <c>ValidateOnStart</c> runs, and it must not fail before that validation can report.
    /// </summary>
    internal static ChronicleFlightRecorderOptions ReadOrDisabled(IOptions<ChronicleOptions> options)
    {
        try
        {
            return options.Value.FlightRecorder;
        }
        catch (OptionsValidationException)
        {
            return Disabled;
        }
    }
}
