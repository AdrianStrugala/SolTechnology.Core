using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Options;
using SolTechnology.Core.API;
using NUnit.Framework;

namespace SolTechnology.Core.API.Tests;

/// <summary>
/// Premortem scenario #7: [ApiController]'s auto-400 for model-binding / DataAnnotations
/// failures runs <em>before</em> MVC filters via <c>ApiBehaviorOptions.InvalidModelStateResponseFactory</c>.
/// Without the factory wrapper installed by <c>AddApiExceptionHandling</c>, those 400 bodies
/// would lack <c>extensions["correlationId"]</c> — a hole in the wire contract that only shows
/// up when a client sends a malformed payload, which is exactly when they need a token to
/// quote in support.
/// </summary>
public sealed class ApiBehaviorCorrelationIdTests
{
    private const string CorrelationIdKey = "correlationId";

    [Test]
    public void Auto400_From_ApiController_Carries_CorrelationId()
    {
        using var host = BuildHost();
        var factory = ResolveFactory(host);

        var actionContext = BuildActionContextWithInvalidModelState(host);
        var response = factory(actionContext);

        // The framework default is BadRequestObjectResult { Value: ValidationProblemDetails }.
        // We assert through the base ObjectResult shape so a future framework swap to a custom
        // result type still passes as long as ProblemDetails is the carrier.
        var objectResult = response.Should().BeAssignableTo<ObjectResult>().Subject;
        var problem = objectResult.Value.Should().BeAssignableTo<ProblemDetails>().Subject;

        problem.Extensions.Should().ContainKey(CorrelationIdKey);
        problem.Extensions[CorrelationIdKey].Should().NotBeNull();
    }

    [Test]
    public void Auto400_Preserves_PerField_Errors()
    {
        // Guard against an accidental rewrite that drops the framework's per-field error map.
        // Clients rely on errors[] to render field-level messages without parsing a blob.
        using var host = BuildHost();
        var factory = ResolveFactory(host);

        var actionContext = BuildActionContextWithInvalidModelState(host);
        var response = factory(actionContext);

        var problem = ((ObjectResult)response).Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey("Email");
        problem.Errors["Email"].Should().NotBeEmpty();
    }

    private static WebApplication BuildHost()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers();
        builder.Services.AddSolApiExceptionHandling();
        return builder.Build();
    }

    private static Func<ActionContext, IActionResult> ResolveFactory(WebApplication host)
    {
        var options = host.Services.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
        return options.InvalidModelStateResponseFactory;
    }

    private static ActionContext BuildActionContextWithInvalidModelState(WebApplication host)
    {
        var httpContext = new DefaultHttpContext { RequestServices = host.Services };
        var ctx = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        ctx.ModelState.AddModelError("Email", "'Email' is not a valid email address.");
        return ctx;
    }
}


