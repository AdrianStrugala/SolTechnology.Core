using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NUnit.Framework;
using SolTechnology.Core.API.HealthChecks;

namespace SolTechnology.Core.API.Tests;

/// <summary>
/// Pins the pure <see cref="HealthReportJsonFormatter"/> contract: overall status + per-check detail,
/// rendered as valid JSON with no <c>HttpContext</c> dependency.
/// </summary>
public sealed class HealthReportJsonFormatterTests
{
    [Test]
    public void Format_EmptyReport_Emits_Healthy_With_No_Entries()
    {
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            TimeSpan.FromMilliseconds(3));

        var json = HealthReportJsonFormatter.Format(report);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        doc.RootElement.GetProperty("entries").EnumerateObject().Should().BeEmpty();
    }

    [Test]
    public void Format_MultiCheckReport_Emits_PerCheck_Detail()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["sql"] = new(HealthStatus.Healthy, "SQL reachable", TimeSpan.FromMilliseconds(5), null, null),
            ["redis"] = new(HealthStatus.Unhealthy, "connection refused", TimeSpan.FromMilliseconds(50), null, null)
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(55));

        var json = HealthReportJsonFormatter.Format(report);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Overall status rolls up to the worst check.
        root.GetProperty("status").GetString().Should().Be("Unhealthy");

        var sql = root.GetProperty("entries").GetProperty("sql");
        sql.GetProperty("status").GetString().Should().Be("Healthy");
        sql.GetProperty("description").GetString().Should().Be("SQL reachable");
        sql.TryGetProperty("duration", out _).Should().BeTrue();

        var redis = root.GetProperty("entries").GetProperty("redis");
        redis.GetProperty("status").GetString().Should().Be("Unhealthy");
        redis.GetProperty("description").GetString().Should().Be("connection refused");
    }

    [Test]
    public void Format_Entry_Without_Description_Omits_The_Field()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["self"] = new(HealthStatus.Healthy, description: null, TimeSpan.Zero, null, null)
        };
        var report = new HealthReport(entries, TimeSpan.Zero);

        var json = HealthReportJsonFormatter.Format(report);

        using var doc = JsonDocument.Parse(json);
        var self = doc.RootElement.GetProperty("entries").GetProperty("self");
        self.TryGetProperty("description", out _).Should().BeFalse();
        self.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Test]
    public void Format_Degraded_Report_Rolls_Up_To_Degraded()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["a"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null),
            ["b"] = new(HealthStatus.Degraded, "slow", TimeSpan.Zero, null, null)
        };
        var report = new HealthReport(entries, TimeSpan.Zero);

        var json = HealthReportJsonFormatter.Format(report);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Degraded");
    }
}

