using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.Logging.Enrichment;
using SolTechnology.Core.Logging.Middleware;

namespace SolTechnology.Core.Logging.Tests.Middleware;

public class LoggingMiddlewareBodyBufferingTests
{
    private sealed class NoOpCorrelationStore : ICorrelationIdService
    {
        public void Set(CorrelationId correlationId) { }
        public CorrelationId? Get() => null;
        public void Clear() { }
        public CorrelationId GetOrGenerate() => CorrelationId.Generate();
    }

    private sealed class CapturingEnricher : ILogScopeEnricher
    {
        public IDictionary<string, object?>? Captured { get; private set; }
        public void Enrich(HttpContext context, IDictionary<string, object?> scope)
            => Captured = new Dictionary<string, object?>(scope);
    }

    private sealed class CapturingLogger : ILogger<LoggingMiddleware>
    {
        public List<(LogLevel Level, EventId EventId)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, eventId));
    }

    private sealed class DisconnectingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new ConnectionResetException("client gone");
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => throw new ConnectionResetException("client gone");
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Test]
    public async Task Invoke_ClientDisconnectsDuringBodyRead_LogsSingleWarningWithoutFinished()
    {
        // Arrange
        var bodyEnricher = new LogDetailEnricher(
            [new LogDetailDescriptor("name", "CityName", LogDetailSource.Body, null)]);
        var logger = new CapturingLogger();
        var sut = new LoggingMiddleware(
            async context => await context.Request.Body.ReadAsync(new byte[16]),
            Options.Create(new LoggingOptions()));
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/cities";
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = 64;
        context.Request.Body = new DisconnectingStream();

        // Act
        var act = () => sut.InvokeAsync(context, logger, new NoOpCorrelationStore(), [bodyEnricher],
            TestChronicles.Create(enabled: false));

        // Assert
        await act.Should().ThrowAsync<ConnectionResetException>();
        logger.Entries.Where(e => e.Level >= LogLevel.Warning).Should().ContainSingle()
            .Which.Should().Be((LogLevel.Warning, new EventId(2144, "RequestAborted")));
        logger.Entries.Select(e => e.EventId.Id).Should().NotContain([2146, 2155]);
    }

    [TestCase("application/json; charset=utf-8", "{\"name\":\"Warsaw\"}", 1024, true)]
    [TestCase("text/plain", "{\"name\":\"Warsaw\"}", 1024, false)]
    [TestCase("application/json", "{\"name\":\"Warsaw\"}", 5, false)]
    [TestCase("application/json", "{not json", 1024, false)]
    public async Task Invoke_BodyEnrichment_ParsesOnlyBoundedJson(
        string contentType, string body, long maxBodyBytes, bool expectedEnriched)
    {
        // Arrange
        var bodyEnricher = new LogDetailEnricher(
            [new LogDetailDescriptor("name", "CityName", LogDetailSource.Body, null)]);
        var capturing = new CapturingEnricher();
        var nextInvoked = false;
        var sut = new LoggingMiddleware(
            _ =>
            {
                nextInvoked = true;
                return Task.CompletedTask;
            },
            Options.Create(new LoggingOptions { MaxLoggedJsonBodyBytes = maxBodyBytes }));
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/cities";
        context.Request.ContentType = contentType;
        var bytes = Encoding.UTF8.GetBytes(body);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;

        // Act
        await sut.InvokeAsync(context, NullLogger<LoggingMiddleware>.Instance, new NoOpCorrelationStore(),
            [bodyEnricher, capturing], TestChronicles.Create(enabled: false));

        // Assert
        nextInvoked.Should().BeTrue();
        capturing.Captured!.ContainsKey("CityName").Should().Be(expectedEnriched);
        context.Request.Body.Position.Should().Be(0);
    }
}
