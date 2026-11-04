using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace SolTechnology.Core.Auth.Tests;

[TestFixture]
public class ApiKeyAuthenticationHandlerTests
{
    private const string ConfiguredKey = "secret-key";

    [TestCase(ConfiguredKey, null, false, TestName = "{m}(missing header)")]
    [TestCase(ConfiguredKey, "wrong-key!", false, TestName = "{m}(wrong key)")]
    [TestCase(ConfiguredKey, ConfiguredKey + "-and-more", false, TestName = "{m}(matching prefix, different length)")]
    [TestCase(ConfiguredKey, ConfiguredKey, true, TestName = "{m}(exact key)")]
    [TestCase("", "", false, TestName = "{m}(configured key empty, header empty)")]
    [TestCase("   ", "   ", false, TestName = "{m}(configured key whitespace)")]
    public async Task HandleAuthenticateAsync_ComparesHeaderWithConfiguredKey(string configuredKey, string? header, bool expectedSuccess)
    {
        // Arrange
        var logs = new CapturingLoggerProvider();
        var handler = await CreateHandler(configuredKey, header, logs);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            result.Principal!.Identity!.IsAuthenticated.Should().BeTrue();
            logs.Entries.Should().NotContain(entry => entry.Level >= LogLevel.Warning);
        }
        else
        {
            result.Failure.Should().NotBeNull();
            logs.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Warning)
                .Which.Message.Should().NotContain(ConfiguredKey).And.NotContain("wrong-key!");
        }
    }

    private static async Task<ApiKeyAuthenticationHandler> CreateHandler(string configuredKey, string? header, ILoggerProvider logs)
    {
        var options = new ApiKeyAuthenticationSchemeOptions { ApiKey = configuredKey };
        var optionsMonitor = Substitute.For<IOptionsMonitor<ApiKeyAuthenticationSchemeOptions>>();
        optionsMonitor.Get(ApiKeyAuthenticationSchemeOptions.AuthenticationScheme).Returns(options);
        var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logs).SetMinimumLevel(LogLevel.Trace));

        var handler = new ApiKeyAuthenticationHandler(optionsMonitor, loggerFactory, UrlEncoder.Default);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/trips";
        if (header is not null)
        {
            context.Request.Headers[ApiKeyAuthenticationSchemeOptions.AuthenticationHeaderName] = header;
        }

        var scheme = new AuthenticationScheme(
            ApiKeyAuthenticationSchemeOptions.AuthenticationScheme,
            displayName: null,
            typeof(ApiKeyAuthenticationHandler));
        await handler.InitializeAsync(scheme, context);
        return handler;
    }

    private sealed record CapturedLog(LogLevel Level, string Message);

    private sealed class CapturingLoggerProvider : ILoggerProvider, ILogger
    {
        public List<CapturedLog> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new CapturedLog(logLevel, formatter(state, exception)));
        }

        public void Dispose()
        {
        }
    }
}
