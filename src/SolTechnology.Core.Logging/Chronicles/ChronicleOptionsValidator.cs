using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>Range checks for <see cref="ChronicleOptions"/>; wired to <c>ValidateOnStart</c> by <c>AddSolChronicle</c>.</summary>
internal sealed class ChronicleOptionsValidator : IValidateOptions<ChronicleOptions>
{
    public ValidateOptionsResult Validate(string? name, ChronicleOptions options)
    {
        var failures = new List<string>();
        Require(failures, options.MaxSteps is >= 1 and <= 10000, "MaxSteps must be between 1 and 10000.");
        Require(failures, options.MaxDepth is >= 1 and <= 64, "MaxDepth must be between 1 and 64.");
        Require(failures, options.MaxAttributesPerStep is >= 0 and <= 64, "MaxAttributesPerStep must be between 0 and 64.");
        Require(failures, options.MaxAttributeValueLength is >= 16 and <= 4096, "MaxAttributeValueLength must be between 16 and 4096.");
        Require(failures, options.AggregateThreshold >= 2, "AggregateThreshold must be at least 2.");
        Require(failures, options.NPlusOneThreshold >= options.AggregateThreshold, "NPlusOneThreshold must be at least AggregateThreshold.");
        Require(failures, options.SlowThreshold > TimeSpan.Zero, "SlowThreshold must be positive.");
        Require(failures, options.SampleRate is >= 0.0 and <= 1.0, "SampleRate must be between 0.0 and 1.0.");

        var recorder = options.FlightRecorder;
        Require(failures, recorder is not null, "FlightRecorder must not be null.");
        if (recorder is not null)
        {
            Require(failures, recorder.MinimumLevel is LogLevel.Trace or LogLevel.Debug, "FlightRecorder.MinimumLevel must be Trace or Debug.");
            Require(failures, recorder.MaxEntries is >= 1 and <= 1000, "FlightRecorder.MaxEntries must be between 1 and 1000.");
            Require(failures, recorder.MaxMessageLength is >= 32 and <= 4096, "FlightRecorder.MaxMessageLength must be between 32 and 4096.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void Require(List<string> failures, bool condition, string message)
    {
        if (!condition)
        {
            failures.Add($"{nameof(ChronicleOptions)}.{message}");
        }
    }
}
