using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using SolTechnology.Core.API.Security;

namespace SolTechnology.Core.API.Tests;

/// <summary>
/// Pins the security-headers middleware contract: strict defaults on every response,
/// relaxed CSP on Swagger/Redoc paths, pre-existing headers never clobbered.
/// </summary>
public sealed class SecurityHeadersMiddlewareTests
{
    [Test]
    public async Task Default_Adds_Strict_Headers_On_Every_Response()
    {
        using var host = await CreateHost(app => app.UseSecurityHeaders());

        var client = host.GetTestClient();
        var response = await client.GetAsync("/api/test");

        response.Headers.GetValues("Content-Security-Policy").Should()
            .ContainSingle("default-src 'none'; frame-ancestors 'none'");
        response.Headers.GetValues("X-Content-Type-Options").Should()
            .ContainSingle("nosniff");
        response.Headers.GetValues("Referrer-Policy").Should()
            .ContainSingle("no-referrer");
    }

    [Test]
    public async Task Swagger_Path_Gets_Relaxed_CSP()
    {
        using var host = await CreateHost(app => app.UseSecurityHeaders());

        var client = host.GetTestClient();
        var response = await client.GetAsync("/swagger/index.html");

        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        csp.Should().Contain("'self'");
        csp.Should().Contain("'unsafe-inline'");
        csp.Should().NotBe("default-src 'none'; frame-ancestors 'none'");
    }

    [Test]
    public async Task Docs_Path_Gets_Relaxed_CSP()
    {
        using var host = await CreateHost(app => app.UseSecurityHeaders());

        var client = host.GetTestClient();
        var response = await client.GetAsync("/docs/v1");

        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        csp.Should().Contain("'self'");
    }

    [Test]
    public async Task PreExisting_Headers_Are_Not_Overwritten()
    {
        using var host = await CreateHost(app =>
        {
            app.Use(async (ctx, next) =>
            {
                ctx.Response.Headers["Content-Security-Policy"] = "custom-policy";
                await next();
            });
            app.UseSecurityHeaders();
        });

        var client = host.GetTestClient();
        var response = await client.GetAsync("/api/test");

        response.Headers.GetValues("Content-Security-Policy").Should()
            .ContainSingle("custom-policy");
        // Other headers still added
        response.Headers.GetValues("X-Content-Type-Options").Should()
            .ContainSingle("nosniff");
    }

    [Test]
    public async Task Custom_Options_Are_Respected()
    {
        using var host = await CreateHost(app => app.UseSecurityHeaders(o =>
        {
            o.ReferrerPolicy = "strict-origin-when-cross-origin";
            o.RelaxedPathPrefixes = ["/custom-docs"];
        }));

        var client = host.GetTestClient();

        var apiResponse = await client.GetAsync("/api/test");
        apiResponse.Headers.GetValues("Referrer-Policy").Should()
            .ContainSingle("strict-origin-when-cross-origin");

        // Default /swagger no longer relaxed (custom list doesn't include it)
        var swaggerResponse = await client.GetAsync("/swagger/index.html");
        swaggerResponse.Headers.GetValues("Content-Security-Policy").Should()
            .ContainSingle("default-src 'none'; frame-ancestors 'none'");

        // Custom path IS relaxed
        var docsResponse = await client.GetAsync("/custom-docs/v1");
        var csp = docsResponse.Headers.GetValues("Content-Security-Policy").Single();
        csp.Should().Contain("'self'");
    }

    [Test]
    public async Task Error_Responses_Also_Receive_Headers()
    {
        using var host = await CreateHost(app =>
        {
            app.UseSecurityHeaders();
            app.Run(_ => throw new InvalidOperationException("boom"));
        });

        var client = host.GetTestClient();
        var response = await client.GetAsync("/api/test");

        // Even on 500 the headers are present (OnStarting fires before body)
        response.Headers.Contains("Content-Security-Policy").Should().BeTrue();
        response.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
    }

    private static async Task<IHost> CreateHost(Action<IApplicationBuilder> configurePipeline)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.Configure(app =>
                {
                    configurePipeline(app);
                    // Terminal middleware for paths that don't throw
                    app.Run(async ctx =>
                    {
                        ctx.Response.StatusCode = 200;
                        await ctx.Response.WriteAsync("ok");
                    });
                });
            })
            .Build();

        await host.StartAsync();
        return host;
    }
}

