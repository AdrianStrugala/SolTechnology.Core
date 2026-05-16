using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SolTechnology.Core.API;
using Xunit;

namespace SolTechnology.Core.API.Tests;

/// <summary>
/// Premortem scenario #6: ProblemDetails emitted outside the MVC pipeline (routing 404,
/// UseStatusCodePages, UseExceptionHandler, auth challenges) must carry the same correlation
/// id as MVC-mapped errors. Without the <c>CustomizeProblemDetails</c> hook the same API would
/// expose two ProblemDetails shapes — one with <c>correlationId</c>, one without — and clients
/// would lose the only token that resolves to logs in Seq / App Insights.
/// </summary>
public sealed class ProblemDetailsCorrelationIdTests
{
    // Wire contract key — pinned as a literal so this test fails loudly if anyone renames the
    // internal constant. The string IS the contract; clients query the body by this exact name.
    private const string CorrelationIdKey = "correlationId";
    [Fact]
    public void Framework_ProblemDetails_Outside_Mvc_Carries_CorrelationId()
    {
        // The framework calls CustomizeProblemDetails through IProblemDetailsService.WriteAsync;
        // asserting through the options is equivalent and isolates the test from MVC-specific
        // plumbing — what matters is that the consumer registered the callback at all and that
        // it populates the correlationId extension from Core.Logging's service.
        using var host = BuildHost();
        var callback = ResolveCustomizeCallback(host);

        var problem = new ProblemDetails { Status = StatusCodes.Status404NotFound };
        callback.Invoke(new ProblemDetailsContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = host.Services },
            ProblemDetails = problem
        });

        problem.Extensions.Should().ContainKey(CorrelationIdKey);
        problem.Extensions[CorrelationIdKey].Should().NotBeNull();
    }

    [Fact]
    public void Existing_CorrelationId_Is_Not_Overwritten()
    {
        // The MVC filters set correlationId before the framework hook runs. The hook must be
        // a no-op when the id is already present so the wire value cannot drift mid-response.
        using var host = BuildHost();
        var callback = ResolveCustomizeCallback(host);

        var problem = new ProblemDetails { Status = StatusCodes.Status500InternalServerError };
        problem.Extensions[CorrelationIdKey] = "preset-id-from-mvc-filter";

        callback.Invoke(new ProblemDetailsContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = host.Services },
            ProblemDetails = problem
        });

        problem.Extensions[CorrelationIdKey].Should().Be("preset-id-from-mvc-filter");
    }

    private static WebApplication BuildHost()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddApiExceptionHandling();
        return builder.Build();
    }

    private static Action<ProblemDetailsContext> ResolveCustomizeCallback(WebApplication host)
    {
        var options = host.Services.GetRequiredService<IOptions<ProblemDetailsOptions>>().Value;
        options.CustomizeProblemDetails.Should().NotBeNull(
            "AddApiExceptionHandling must configure CustomizeProblemDetails for non-MVC paths");
        return options.CustomizeProblemDetails!;
    }
}







