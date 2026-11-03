using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.Logging.Enrichment;
using SolTechnology.Core.Logging.Middleware;

namespace SolTechnology.Core.Logging.Tests.Middleware;

public class LoggingMiddlewareEnvelopeTests
{
    private sealed class TemplateCapturingLogger : ILogger<LoggingMiddleware>
    {
        public List<(LogLevel Level, string Template, string Message, bool HasException)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var template = state is IReadOnlyList<KeyValuePair<string, object?>> values
                ? values.First(v => v.Key == "{OriginalFormat}").Value as string
                : null;
            Entries.Add((logLevel, template ?? string.Empty, formatter(state, exception), exception is not null));
        }
    }

    private sealed class NoOpCorrelationStore : ICorrelationIdService
    {
        public void Set(CorrelationId correlationId) { }
        public CorrelationId? Get() => null;
        public CorrelationId GetOrGenerate() => CorrelationId.Generate();
    }

    private sealed class ThrowingEnricher : ILogScopeEnricher
    {
        public void Enrich(HttpContext context, IDictionary<string, object?> scope)
            => throw new InvalidOperationException("boom");
    }

    private static async Task<TemplateCapturingLogger> Invoke(
        RequestDelegate next, ILogScopeEnricher[]? enrichers = null, Action<ITimingService>? useTiming = null,
        string? correlationHeader = null)
    {
        var logger = new TemplateCapturingLogger();
        var timing = new TimingService(TimeProvider.System);
        var sut = new LoggingMiddleware(
            async context =>
            {
                useTiming?.Invoke(timing);
                await next(context);
            },
            Options.Create(new LoggingOptions()));
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/trips";
        if (correlationHeader is not null)
        {
            context.Request.Headers[CorrelationId.HeaderKey] = correlationHeader;
        }

        try
        {
            await sut.InvokeAsync(context, logger, new NoOpCorrelationStore(), enrichers ?? [], timing);
        }
        catch (InvalidOperationException)
        {
        }

        return logger;
    }

    [TestCase(StatusCodes.Status200OK, LogLevel.Information)]
    [TestCase(StatusCodes.Status404NotFound, LogLevel.Warning)]
    [TestCase(StatusCodes.Status503ServiceUnavailable, LogLevel.Error)]
    public async Task Invoke_CompletedRequest_LogsStartedAndFinishedTemplates(int statusCode, LogLevel finishedLevel)
    {
        // Act
        var logger = await Invoke(context =>
        {
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        });

        // Assert
        logger.Entries.Select(e => (e.Level, e.Template)).Should().Equal(
            (LogLevel.Information, "Started request [{RequestMethod}] [{RequestPath}]"),
            (finishedLevel, "Finished request [{RequestMethod}] [{RequestPath}] -> [{StatusCode}] in [{ElapsedMs} ms]"));
        logger.Entries[0].Message.Should().Be("Started request [GET] [/api/trips]");
        logger.Entries[1].Message.Should().StartWith($"Finished request [GET] [/api/trips] -> [{statusCode}] in [");
    }

    [Test]
    public async Task Invoke_TimedRequest_LogsFinishedWithTimingsTemplate()
    {
        // Act
        var logger = await Invoke(_ => Task.CompletedTask, useTiming: timing => timing.StartContext("db").Dispose());

        // Assert
        logger.Entries.Last().Template.Should().Be(
            "Finished request [{RequestMethod}] [{RequestPath}] -> [{StatusCode}] in [{ElapsedMs} ms] — timings: [{@Timings}]");
    }

    [Test]
    public async Task Invoke_FailingRequest_LogsFailedTemplateWithException()
    {
        // Act
        var logger = await Invoke(_ => throw new InvalidOperationException("boom"));

        // Assert
        logger.Entries.Last().Should().Match<(LogLevel Level, string Template, string Message, bool HasException)>(e =>
            e.Level == LogLevel.Error
            && e.Template == "Request [{RequestMethod}] [{RequestPath}] failed after [{ElapsedMs} ms]"
            && e.HasException);
    }

    [Test]
    public async Task Invoke_RejectedHeaderAndFaultyEnricher_LogsWarningTemplates()
    {
        // Act
        var logger = await Invoke(_ => Task.CompletedTask, [new ThrowingEnricher()], correlationHeader: "<script>");

        // Assert
        logger.Entries.Where(e => e.Level == LogLevel.Warning).Select(e => e.Template).Should().Equal(
            "Log scope enricher [{EnricherType}] threw and was skipped",
            "Invalid [{Header}] header rejected: [{CorrelationError}]");
    }
}
