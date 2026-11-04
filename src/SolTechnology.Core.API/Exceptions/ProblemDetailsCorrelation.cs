using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.API.Exceptions;

/// <summary>
/// Stamps <c>Extensions["correlationId"]</c> on <see cref="ProblemDetails"/> produced outside the MVC filters
/// (routing 404, status-code pages, exception handler, auth challenges, <c>[ApiController]</c> model
/// validation), so every error body matches what <c>ExceptionFilter</c> and <c>ResultConversionFilter</c> emit.
/// </summary>
internal static class ProblemDetailsCorrelation
{
    public static void Stamp(ProblemDetailsContext context) =>
        TryStamp(context.ProblemDetails, context.HttpContext.RequestServices);

    /// <summary>
    /// Wraps the default <c>InvalidModelStateResponseFactory</c> without disturbing its per-field errors dictionary.
    /// </summary>
    public static Func<ActionContext, IActionResult> StampInvalidModelState(Func<ActionContext, IActionResult> defaultFactory) =>
        actionContext =>
        {
            var response = defaultFactory(actionContext);
            if (response is ObjectResult { Value: ProblemDetails problem })
            {
                TryStamp(problem, actionContext.HttpContext.RequestServices);
            }

            return response;
        };

    private static void TryStamp(ProblemDetails problem, IServiceProvider requestServices)
    {
        if (problem.Extensions.ContainsKey(ApiProblemDetailsFactory.CorrelationIdKey))
        {
            return;
        }

        var correlationIdService = requestServices.GetService<ICorrelationIdService>();
        if (correlationIdService is not null)
        {
            problem.Extensions[ApiProblemDetailsFactory.CorrelationIdKey] = correlationIdService.GetOrGenerate().Value;
        }
    }
}
