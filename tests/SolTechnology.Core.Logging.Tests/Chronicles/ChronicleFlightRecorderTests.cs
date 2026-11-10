using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Chronicles.FlightRecorder;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleFlightRecorderTests : ChronicleTestBase
{
    private ServiceProvider? _provider;

    [TearDown]
    public void DisposeProvider() => _provider?.Dispose();

    [Test]
    public void FailedRoot_DebugEntries_AttachedToReport()
    {
        // Arrange
        var (chronicle, logger) = Build(o => o.FlightRecorder.Enabled = true);

        // Act
        using (var root = chronicle.Begin("Import"))
        {
            Time.Advance(TimeSpan.FromMilliseconds(3));
            logger.LogDebug(new EventId(7), new InvalidOperationException("inner"), "Loading [{City}]", "Krakow");
            root.Fail("Validation");
        }

        // Assert
        var report = Report();
        var record = report.FlightRecords.Should().ContainSingle().Subject;
        record.Should().BeEquivalentTo(new { OffsetMs = 3L, Level = LogLevel.Debug, Category = "App", EventId = 7, Message = "Loading [Krakow]", ExceptionType = nameof(InvalidOperationException) });
        report.ToString().Should().EndWith("\nflight recorder:\n  [@3 ms] [Debug] App: Loading [Krakow] (InvalidOperationException)");
    }

    [Test]
    public void SucceededRoot_DebugEntries_Dropped()
    {
        // Arrange
        var (chronicle, logger) = Build(o => o.FlightRecorder.Enabled = true);

        // Act
        using (chronicle.Begin("Import"))
        {
            logger.LogDebug("Loading [{City}]", "Krakow");
        }

        // Assert
        Report().FlightRecords.Should().BeEmpty();
        Report().ToString().Should().NotContain("flight recorder");
    }

    [Test]
    public void NoRoot_IsEnabledDebug_ReturnsFalse()
    {
        // Arrange
        var (_, logger) = Build(o => o.FlightRecorder.Enabled = true);

        // Act
        var isEnabled = logger.IsEnabled(LogLevel.Debug);

        // Assert
        isEnabled.Should().BeFalse();
    }

    [Test]
    public void RecorderDisabled_IsEnabledDebug_ReturnsFalse()
    {
        // Arrange
        var (chronicle, _) = Build(o => o.FlightRecorder.Enabled = false);
        var sut = RecorderProvider().CreateLogger("App");

        // Act
        using (chronicle.Begin("Import"))
        {
            // Assert
            sut.IsEnabled(LogLevel.Debug).Should().BeFalse();
        }
    }

    [Test]
    public void MaxEntries_Exceeded_KeepsFirstAndCountsDropped()
    {
        // Arrange
        var (chronicle, logger) = Build(o => (o.FlightRecorder.Enabled, o.FlightRecorder.MaxEntries) = (true, 2));

        // Act
        using (var root = chronicle.Begin("Import"))
        {
            for (var i = 0; i < 5; i++)
            {
                logger.LogDebug("Entry [{Index}]", i);
            }

            root.Fail("Boom");
        }

        // Assert
        var report = Report();
        report.FlightRecords.Select(r => r.Message).Should().Equal("Entry [0]", "Entry [1]");
        report.ToString().Should().EndWith("\n  +3 records dropped");
    }

    [Test]
    public void MaxEntries_Reached_FormatterNotInvoked()
    {
        // Arrange
        var (chronicle, _) = Build(o => (o.FlightRecorder.Enabled, o.FlightRecorder.MaxEntries) = (true, 1));
        var sut = RecorderProvider().CreateLogger("App");
        var formatted = 0;

        // Act
        using (chronicle.Begin("Import"))
        {
            for (var i = 0; i < 3; i++)
            {
                sut.Log(LogLevel.Debug, default, i, null, (state, _) => $"Entry [{state}] #{++formatted}");
            }
        }

        // Assert
        formatted.Should().Be(1);
    }

    [TestCase("Microsoft.EntityFrameworkCore", LogLevel.Debug)]
    [TestCase("System.Net.Http", LogLevel.Debug)]
    [TestCase(ChronicleLogMessages.Category, LogLevel.Debug)]
    [TestCase("App", LogLevel.Information)]
    public void EntryOutsideRecordedLevelsOrCategories_NotBuffered(string category, LogLevel level)
    {
        // Arrange
        var (chronicle, _) = Build(o => o.FlightRecorder.Enabled = true);
        var logger = _provider!.GetRequiredService<ILoggerFactory>().CreateLogger(category);

        // Act
        using (var root = chronicle.Begin("Import"))
        {
            logger.Log(level, "Entry");
            root.Fail("Boom");
        }

        // Assert
        Report().FlightRecords.Should().BeEmpty();
    }

    [Test]
    public void UserRuleForAlias_WinsOverDefaults()
    {
        // Arrange
        var (chronicle, logger) = Build(
            o => o.FlightRecorder.Enabled = true,
            logging => logging.Services.Configure<LoggerFilterOptions>(filter =>
                filter.Rules.Add(new LoggerFilterRule(ChronicleFlightRecorderProvider.Alias, "Microsoft", LogLevel.Debug, null))));
        var microsoft = _provider!.GetRequiredService<ILoggerFactory>().CreateLogger("Microsoft.EntityFrameworkCore");

        // Act
        using (var root = chronicle.Begin("Import"))
        {
            microsoft.LogDebug("Executing query");
            logger.LogDebug("Loading city");
            root.Fail("Boom");
        }

        // Assert
        Report().FlightRecords.Should().ContainSingle().Which.Category.Should().Be("Microsoft.EntityFrameworkCore");
    }

    [Test]
    public void FlightRecorderDisabled_ProviderNotConsultedForDebug()
    {
        // Arrange
        var (chronicle, logger) = Build(o => o.FlightRecorder.Enabled = false);

        // Act
        using (chronicle.Begin("Import"))
        {
            for (var i = 0; i < 1_000; i++)
            {
                _ = logger.IsEnabled(LogLevel.Debug);
                logger.LogDebug("Entry [{Index}]", i);
            }
        }

        // Assert
        RecorderProvider().Consultations.Should().Be(0);
    }

    [Test]
    public void AddSolChronicle_WithFlightRecorder_ResolvesLoggerFactoryWithoutCycle()
    {
        // Arrange
        var (chronicle, logger) = Build(o => o.FlightRecorder.Enabled = true);

        // Act
        var providers = _provider!.GetServices<ILoggerProvider>();

        // Assert
        chronicle.Should().BeOfType<ChronicleRecorder>();
        logger.Should().NotBeNull();
        providers.Should().ContainSingle(p => p is ChronicleFlightRecorderProvider);
    }

    private (IChronicle Chronicle, ILogger Logger) Build(Action<ChronicleOptions> configure, Action<ILoggingBuilder>? logging = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Logging:LogLevel:Default"] = "Information" })
            .Build();
        var services = new ServiceCollection()
            .AddSingleton(TimeProvider)
            .AddSingleton<IChronicleRootListener>(Listener)
            .AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                logging?.Invoke(builder);
            })
            .AddSolChronicle(configure);
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        return (_provider.GetRequiredService<IChronicle>(), _provider.GetRequiredService<ILoggerFactory>().CreateLogger("App"));
    }

    private TimeProvider TimeProvider => Time;

    private ChronicleFlightRecorderProvider RecorderProvider()
        => _provider!.GetServices<ILoggerProvider>().OfType<ChronicleFlightRecorderProvider>().Single();

    private ChronicleReport Report() => ChronicleReportBuilder.Build(Listener.Completed.Single());
}
