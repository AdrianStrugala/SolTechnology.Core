using System.Text;
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
        public List<(LogLevel Level, string Template, string Message, bool HasException, int EventId)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var template = state is IReadOnlyList<KeyValuePair<string, object?>> values
                ? values.First(v => v.Key == "{OriginalFormat}").Value as string
                : null;
            Entries.Add((logLevel, template ?? string.Empty, formatter(state, exception), exception is not null, eventId.Id));
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
        string? correlationHeader = null, Action<HttpContext>? arrangeContext = null)
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

        arrangeContext?.Invoke(context);
        try
        {
            await sut.InvokeAsync(context, logger, new NoOpCorrelationStore(), enrichers ?? [], timing);
        }
        catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException)
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
        logger.Entries.Select(e => (e.Level, e.Template, e.EventId)).Should().Equal(
            (LogLevel.Information, "Started request [{RequestMethod}] [{RequestPath}]", 2143),
            (finishedLevel, "Finished request [{RequestMethod}] [{RequestPath}] -> [{StatusCode}] in [{ElapsedMs} ms]", 2146));
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
        logger.Entries.Last().EventId.Should().Be(2147);
    }

    [Test]
    public async Task Invoke_FailingRequest_LogsFailedTemplateWithException()
    {
        // Act
        var logger = await Invoke(_ => throw new InvalidOperationException("boom"));

        // Assert
        logger.Entries.Last().Should().Match<(LogLevel Level, string Template, string Message, bool HasException, int EventId)>(e =>
            e.Level == LogLevel.Error
            && e.Template == "Request [{RequestMethod}] [{RequestPath}] failed after [{ElapsedMs} ms]"
            && e.HasException
            && e.EventId == 2145);
    }

    [Test]
    public async Task Invoke_RejectedHeaderAndFaultyEnricher_LogsWarningTemplates()
    {
        // Act
        var logger = await Invoke(_ => Task.CompletedTask, [new ThrowingEnricher()], correlationHeader: "<script>");

        // Assert
        logger.Entries.Where(e => e.Level == LogLevel.Warning).Select(e => (e.Template, e.EventId)).Should().Equal(
            ("Log scope enricher [{EnricherType}] threw and was skipped", 2141),
            ("Invalid [{Header}] header rejected: [{CorrelationError}]", 2142));
    }

    [Test]
    public async Task Invoke_ClientAbortedRequest_LogsSingleWarningWithoutFinished()
    {
        // Arrange
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();

        // Act
        var logger = await Invoke(
            context => throw new OperationCanceledException(context.RequestAborted),
            arrangeContext: context => context.RequestAborted = aborted.Token);

        // Assert
        logger.Entries.Where(e => e.Level >= LogLevel.Warning).Should().ContainSingle().Which.Should()
            .Match<(LogLevel Level, string Template, string Message, bool HasException, int EventId)>(e =>
                e.Level == LogLevel.Warning
                && e.Template == "Request [{RequestMethod}] [{RequestPath}] aborted by client after [{ElapsedMs} ms]"
                && e.HasException
                && e.EventId == 2144);
        logger.Entries.Should().NotContain(e => e.Template.StartsWith("Finished request"));
    }

    [Test]
    public async Task Invoke_MalformedJsonBodyForBodyEnricher_LogsDebugSkipTemplate()
    {
        // Arrange
        var bodyEnricher = new LogDetailEnricher(
            [new LogDetailDescriptor("name", "CityName", LogDetailSource.Body, null)]);
        var body = Encoding.UTF8.GetBytes("{not json");

        // Act
        var logger = await Invoke(_ => Task.CompletedTask, [bodyEnricher], arrangeContext: context =>
        {
            context.Request.Method = "POST";
            context.Request.ContentType = "application/json";
            context.Request.ContentLength = body.Length;
            context.Request.Body = new MemoryStream(body);
        });

        // Assert
        logger.Entries.Should().ContainSingle(e => e.EventId == 2148).Which.Should()
            .Match<(LogLevel Level, string Template, string Message, bool HasException, int EventId)>(e =>
                e.Level == LogLevel.Debug
                && e.Template == "Skipping body-based log enrichment for [{RequestPath}] - JSON could not be parsed"
                && e.HasException);
    }
}
