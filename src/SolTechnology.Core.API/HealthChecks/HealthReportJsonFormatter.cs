using System.Buffers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SolTechnology.Core.API.HealthChecks;

/// <summary>
/// Renders a <see cref="HealthReport"/> as JSON. Pure formatter — it has <b>no</b>
/// <c>HttpContext</c> dependency, so it is independently unit-testable and reusable by any host.
/// The ASP.NET adapter that calls it lives in
/// <see cref="HealthChecksEndpointExtensions.MapCoreHealthChecks"/>.
/// </summary>
public static class HealthReportJsonFormatter
{
    /// <summary>
    /// Formats the report as a JSON string: overall <c>status</c>, <c>totalDuration</c>, and a
    /// per-check <c>entries</c> object (name → status/description/duration).
    /// </summary>
    public static string Format(HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            Write(writer, report);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Writes the report JSON directly to a <see cref="Utf8JsonWriter"/> — the allocation-free path
    /// the endpoint adapter uses against the response body.
    /// </summary>
    public static void Write(Utf8JsonWriter writer, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);

        writer.WriteStartObject();
        writer.WriteString("status", report.Status.ToString());
        writer.WriteString("totalDuration", report.TotalDuration.ToString());

        writer.WriteStartObject("entries");
        foreach (var entry in report.Entries)
        {
            writer.WriteStartObject(entry.Key);
            writer.WriteString("status", entry.Value.Status.ToString());

            if (entry.Value.Description is not null)
            {
                writer.WriteString("description", entry.Value.Description);
            }

            writer.WriteString("duration", entry.Value.Duration.ToString());
            writer.WriteEndObject();
        }
        writer.WriteEndObject();

        writer.WriteEndObject();
    }
}

