using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleReporterTests : ChronicleReportingTestBase
{
    [Test]
    public void RootCompleted_EmitsExactlyOneEntryWithEventId2154()
    {
        // Arrange
        var sut = CreateReportingRecorder();

        // Act
        using (sut.Begin("ImportCities"))
        using (sut.Step("Db"))
        {
            Time.Advance(TimeSpan.FromMilliseconds(12));
        }

        // Assert
        var entry = Logs.GetSnapshot().Should().ContainSingle().Subject;
        entry.Id.Id.Should().Be(2154);
        entry.Id.Name.Should().Be("ChronicleReported");
        entry.Level.Should().Be(LogLevel.Information);
        entry.Category.Should().Be("SolTechnology.Core.Logging.Chronicle");
        entry.Message.Should().Be("Chronicle [ImportCities] [Ok] in [12 ms], [2] steps: [\nImportCities [@0 ms] [12 ms] [ok]\n`- Db [@0 ms] [12 ms] [ok]]");
        entry.Exception.Should().BeNull();
    }

    [Test]
    public void FailedRoot_EmitsErrorWithRootException()
    {
        // Arrange
        var sut = CreateReportingRecorder();
        var exception = new InvalidOperationException("boom");

        // Act
        using (var root = sut.Begin("ImportCities"))
        {
            root.Fail(exception);
        }

        // Assert
        var entry = Logs.GetSnapshot().Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Error);
        entry.Exception.Should().BeSameAs(exception);
        entry.Message.Should().StartWith("Chronicle [ImportCities] [Failed]");
    }

    [Test]
    public void ReportedByCallerRoot_NotEmittedByReporter()
    {
        // Arrange
        var sut = CreateReportingRecorder();

        // Act
        using (sut.Begin("GET /cities"))
        {
            AmbientChronicle.Current!.Root.IsReportedByCaller = true;
        }

        // Assert
        Logs.GetSnapshot().Should().BeEmpty();
    }

    [Test]
    public void CategoryAboveLevel_OkRoot_ReportNotBuilt()
    {
        // Arrange
        var builds = 0;
        var sut = CreateReportingRecorder(minimumLevel: LogLevel.Warning, buildReport: root =>
        {
            builds++;
            return ChronicleReportBuilder.Build(root);
        });

        // Act
        using (sut.Begin("ImportCities"))
        using (sut.Step("Db"))
        {
        }

        // Assert
        builds.Should().Be(0);
        Logs.GetSnapshot().Should().BeEmpty();
    }

    [Test]
    public void ChronicleReported_State_CarriesChronicleReportObject()
    {
        // Arrange
        var provider = new StateCapturingLoggerProvider();
        var sut = CreateReportingRecorder(provider: provider);

        // Act
        using (sut.Begin("ImportCities"))
        {
        }

        // Assert
        var (_, eventId, state) = provider.Entries.Should().ContainSingle().Subject;
        eventId.Id.Should().Be(2154);
        state.Should().ContainSingle(p => p.Key == "Chronicle").Which.Value.Should().BeOfType<ChronicleReport>()
            .Which.Name.Should().Be("ImportCities");
        state.Should().Contain(p => p.Key == "ChronicleStatus" && Equals(p.Value, "Ok"));
    }

    [Test]
    public void Template_EveryPlaceholderBracketed()
    {
        // Arrange
        var method = typeof(ChronicleLogMessages).GetMethod(nameof(ChronicleLogMessages.ChronicleReported), BindingFlags.Static | BindingFlags.NonPublic)!;

        // Act
        var template = method.GetCustomAttribute<LoggerMessageAttribute>()!.Message;

        // Assert
        Regex.Matches(template, @"\{[^}]+\}").Should().HaveCount(5)
            .And.OnlyContain(m => template[m.Index - 1] == '[' && "] ".Contains(template[m.Index + m.Length]));
    }

    [Test]
    public void Root_TenSteps_Emitted_AllocatesUnderCeiling()
    {
        // Arrange
        var sut = CreateReportingRecorder(provider: new StateCapturingLoggerProvider { Capture = false });
        void RunRoot()
        {
            using var root = sut.Begin("ImportCities");
            for (var i = 0; i < 10; i++)
            {
                using var step = sut.Step("Db");
            }
        }

        for (var i = 0; i < 20; i++)
        {
            RunRoot();
        }

        // Act
        var before = GC.GetAllocatedBytesForCurrentThread();
        RunRoot();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Assert
        TestContext.Out.WriteLine($"Root with ten steps, emitted: [{allocated}] bytes");
        allocated.Should().BeLessThan(AllocationCeilingBytes);
    }

    // First green measurement (8,808 bytes, 2026-11-10) + 25%, see step 04 Deviations.
    private const long AllocationCeilingBytes = 11_010;
}
