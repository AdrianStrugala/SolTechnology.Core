using System.Globalization;
using System.Text;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// Renders a <see cref="ChronicleReport"/> as an ASCII tree (<c>|-</c>, <c>`-</c>) that every
/// sink and encoding can show. The output starts with a new line and never exceeds
/// <see cref="Budget"/> characters: lines beyond it become one <c>+{n} lines truncated</c> marker.
/// </summary>
internal static class ChronicleTreeRenderer
{
    internal const int Budget = 32_768;
    private const int MarkerReserve = 64;

    internal static string Render(ChronicleReport report)
    {
        var builder = new StringBuilder();
        var truncatedLines = 0;
        foreach (var line in Lines(report))
        {
            if (builder.Length == 0)
            {
                builder.Append('\n').Append(ChronicleText.Truncate(line, Budget - MarkerReserve * 2));
            }
            else if (truncatedLines > 0 || builder.Length + 1 + line.Length > Budget - MarkerReserve)
            {
                truncatedLines++;
            }
            else
            {
                builder.Append('\n').Append(line);
            }
        }

        if (truncatedLines > 0)
        {
            builder.Append(CultureInfo.InvariantCulture, $"\n+{truncatedLines} lines truncated");
        }

        return builder.ToString();
    }

    private static IEnumerable<string> Lines(ChronicleReport report)
    {
        foreach (var root in report.Steps)
        {
            yield return Line(root);
            foreach (var line in ChildLines(root, string.Empty))
            {
                yield return line;
            }
        }

        if (report.DroppedStepCount > 0)
        {
            yield return Invariant($"+{report.DroppedStepCount} steps dropped");
        }

        if (report.LateStepCount > 0)
        {
            yield return Invariant($"+{report.LateStepCount} late steps");
        }
    }

    private static IEnumerable<string> ChildLines(ChronicleReportStep parent, string indent)
    {
        for (var i = 0; i < parent.Children.Count; i++)
        {
            var isLast = i == parent.Children.Count - 1;
            var child = parent.Children[i];
            yield return indent + (isLast ? "`- " : "|- ") + Line(child);
            foreach (var line in ChildLines(child, indent + (isLast ? "   " : "|  ")))
            {
                yield return line;
            }
        }
    }

    private static string Line(ChronicleReportStep step)
    {
        var line = new StringBuilder(Clean(step.Name));
        if (step.IsAggregated)
        {
            line.Append(CultureInfo.InvariantCulture, $" x{step.Count} [total {step.DurationMs} ms] [max {step.MaxDurationMs} ms] [failed {step.FailedCount}]");
            AppendIf(line, step.IsNPlusOneSuspected, " N+1 suspected");
            AppendIf(line, step.NestedCount > 0, Invariant($" +{step.NestedCount} nested"));
        }
        else
        {
            line.Append(CultureInfo.InvariantCulture, $" [@{step.OffsetMs} ms] [{step.DurationMs} ms] [{StatusText(step.Status)}]");
            AppendIf(line, step.ErrorType is not null, $" Error=[{Clean(step.ErrorType)}{(step.ErrorMessage is null ? "" : ": " + Clean(step.ErrorMessage))}]");
            foreach (var (key, value) in step.Attributes)
            {
                line.Append(' ').Append(Clean(key)).Append('=').Append(Clean(value));
            }

            AppendIf(line, step.DroppedAttributeCount > 0, Invariant($" +{step.DroppedAttributeCount} attributes dropped"));
            AppendIf(line, step.DroppedDeeperCount > 0, Invariant($" +{step.DroppedDeeperCount} deeper steps"));
        }

        AppendIf(line, step.IsUnfinished, " (unfinished)");
        return line.ToString();
    }

    private static void AppendIf(StringBuilder line, bool condition, string text)
    {
        if (condition)
        {
            line.Append(text);
        }
    }

    private static string StatusText(ChronicleStepStatus status) => status switch
    {
        ChronicleStepStatus.Failed => "failed",
        ChronicleStepStatus.Cancelled => "cancelled",
        _ => "ok"
    };

    // A line break inside a name or value would break the tree into unrelated lines.
    private static string Clean(string? text)
        => text is null || text.AsSpan().IndexOfAny('\r', '\n', '\t') < 0 ? text ?? string.Empty : text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
