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
