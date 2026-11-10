using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// Turns every completed root into one 2154 entry under <see cref="ChronicleLogMessages.Category"/>,
/// unless the root's owner reports it itself (HTTP request roots).
/// </summary>
internal sealed class ChronicleReporter(ILoggerFactory loggerFactory, IOptions<ChronicleOptions> options) : IChronicleRootListener
{
    private readonly ILogger _logger = loggerFactory.CreateLogger(ChronicleLogMessages.Category);

    internal ChronicleEmissionPolicy Policy { get; init; } = new(options.Value);
    internal Func<ChronicleRoot, ChronicleReport> BuildReport { get; init; } = ChronicleReportBuilder.Build;

    public void OnRootCompleted(ChronicleRoot root)
    {
        if (root.IsReportedByCaller)
        {
            return;
        }

        try
        {
            Report(root);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Chronicle could not report [{ChronicleName}]", root.Node.Name);
        }
    }

    private void Report(ChronicleRoot root)
    {
        var level = Policy.LevelOf(root.Node.Status, root.Duration, hasFailedSteps: root.HasFailedSteps);
        if (!_logger.IsEnabled(level))
        {
            return;
        }

        var report = BuildReport(root);
        if (Policy.ShouldEmit(report, level))
        {
            ChronicleLogMessages.ChronicleReported(
                _logger, level, root.Exception, report.Name, report.Status.ToString(), report.DurationMs, report.StepCount, report);
        }
    }
}
