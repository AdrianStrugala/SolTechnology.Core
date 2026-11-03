using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.Logging.Tests.Correlations;

public sealed class CorrelationIdHostTests
{
    private const string InboundTraceId = "0af7651916cd43dd8448eb211c80319c";
    private const string InboundTraceParent = $"00-{InboundTraceId}-b7ad6b7169203331-01";

    // Hosting starts a request Activity only when something listens; an enabled logger is enough.
    private sealed class EnabledLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new EnabledLogger();
        public void Dispose() { }

        private sealed class EnabledLogger : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) { }
        }
    }

    private static async Task<IHost> StartHost()
    {
        var host = new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureLogging(logging => logging.AddProvider(new EnabledLoggerProvider()))
                .ConfigureServices(services => services.AddSolLogging())
                .Configure(app =>
                {
                    app.UseSolLogging();
                    app.Run(context => context.Response.WriteAsync(Activity.Current?.TraceId.ToHexString() ?? string.Empty));
                }))
            .Build();
        await host.StartAsync();
        return host;
    }

    [Test]
    public async Task Request_WithOnlyCorrelationHeader_UsesAndEchoesHeader()
    {
        // Arrange
        Activity.Current = null;
        using var host = await StartHost();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/trips");
        request.Headers.Add(CorrelationId.HeaderKey, "support-ticket-42");

        // Act
        var response = await host.GetTestClient().SendAsync(request);

        // Assert
        var requestTraceId = await response.Content.ReadAsStringAsync();
        requestTraceId.Should().MatchRegex("^[0-9a-f]{32}$", "hosting must have started a request Activity");
        response.Headers.GetValues(CorrelationId.HeaderKey).Should().Equal("support-ticket-42");
        response.Headers.Contains(CorrelationId.TraceParentHeaderKey).Should().BeFalse("no span id is invented for a header id");
    }

    [Test]
    public async Task Request_WithTraceParent_UsesInboundTraceId()
    {
        // Arrange
        Activity.Current = null;
        using var host = await StartHost();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/trips");
        request.Headers.Add(CorrelationId.TraceParentHeaderKey, InboundTraceParent);
        request.Headers.Add(CorrelationId.HeaderKey, "support-ticket-42");

        // Act
        var response = await host.GetTestClient().SendAsync(request);

        // Assert
        (await response.Content.ReadAsStringAsync()).Should().Be(InboundTraceId);
        response.Headers.GetValues(CorrelationId.HeaderKey).Should().Equal(InboundTraceId);
        response.Headers.GetValues(CorrelationId.TraceParentHeaderKey).Single().Should().StartWith($"00-{InboundTraceId}-");
    }
}
