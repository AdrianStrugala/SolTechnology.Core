using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

/// <summary>Builds a recorder whose only listener is a real <see cref="ChronicleReporter"/>.</summary>
public abstract class ChronicleReportingTestBase : ChronicleTestBase
{
    protected FakeLogCollector Logs { get; private set; } = null!;

    [SetUp]
    public void ResetLogs() => Logs = new FakeLogCollector();

    internal ChronicleRecorder CreateReportingRecorder(
        Action<ChronicleOptions>? configure = null,
        LogLevel minimumLevel = LogLevel.Trace,
        ILoggerProvider? provider = null,
        Func<ChronicleRoot, ChronicleReport>? buildReport = null)
    {
        var options = new ChronicleOptions();
        configure?.Invoke(options);
        var loggerFactory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(minimumLevel)
            .AddProvider(provider ?? new FakeLoggerProvider(Logs)));
        var reporter = new ChronicleReporter(loggerFactory, Options.Create(options))
        {
            Policy = new ChronicleEmissionPolicy(options, () => 0.5),
            BuildReport = buildReport ?? ChronicleReportBuilder.Build
        };
        return new ChronicleRecorder(Options.Create(options), Time, [reporter]);
    }
}
