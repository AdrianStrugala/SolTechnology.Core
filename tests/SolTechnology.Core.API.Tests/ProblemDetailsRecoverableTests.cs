using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SolTechnology.Core.API.Exceptions;
using SolTechnology.Core.API.Filters;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Logging.Correlations;
using NUnit.Framework;

namespace SolTechnology.Core.API.Tests;

/// <summary>
/// Asserts that every <c>ProblemDetails</c> response carries <c>extensions.recoverable</c> — the
/// retry-ability hint surfaced from <see cref="Error.Recoverable"/> (Result path) or derived from
/// the mapped status code (exception path). Contract: always present, never ambiguous.
/// Tests go through the pipeline filters (factory is internal — no InternalsVisibleTo).
/// </summary>
[TestFixture]
public sealed class ProblemDetailsRecoverableTests
{
    // ---- Result / FromError path (via ResultConversionFilter) ----

    [Test]
    public async Task FromError_Recoverable_True_Is_Surfaced_In_ProblemDetails()
    {
        var error = new Error { Message = "Transient blip", Recoverable = true };
        var problem = await GetProblemFromResult(Result<string>.Fail(error));

        problem.Extensions.Should().ContainKey("recoverable");
        problem.Extensions["recoverable"].Should().Be(true);
    }

    [Test]
    public async Task FromError_Recoverable_False_Is_Surfaced_In_ProblemDetails()
    {
        var error = new NotFoundError { Message = "Trip 42 not found.", Recoverable = false };
        var problem = await GetProblemFromResult(Result<string>.Fail(error));

        problem.Extensions.Should().ContainKey("recoverable");
        problem.Extensions["recoverable"].Should().Be(false);
    }

    [Test]
    public async Task FromError_Recoverable_Default_Is_False()
    {
        // Error.Recoverable defaults to false (init-only, no explicit set).
        var error = new ConflictError { Message = "Already exists." };
        var problem = await GetProblemFromResult(Result<string>.Fail(error));

        problem.Extensions["recoverable"].Should().Be(false);
    }

    [Test]
    public async Task FromError_ValidationError_Carries_Recoverable()
    {
        var error = new ValidationError
        {
            Message = "Invalid input.",
            Recoverable = false,
            Errors = new Dictionary<string, string[]>
            {
                ["email"] = ["not valid"]
            }
        };
        var problem = await GetProblemFromResult(Result<string>.Fail(error));

        problem.Extensions.Should().ContainKey("recoverable");
        problem.Extensions["recoverable"].Should().Be(false);
    }

    // ---- Exception / FromException path (via ExceptionFilter) ----

    [Test]
    public void FromException_Mapped_4xx_Is_Not_Recoverable()
    {
        var problem = GetProblemFromException(
            new KeyNotFoundException("not found"),
            StatusCodes.Status404NotFound);

        problem.Extensions.Should().ContainKey("recoverable");
        problem.Extensions["recoverable"].Should().Be(false);
    }

    [Test]
    public void FromException_Unmapped_5xx_Is_Recoverable()
    {
        var problem = GetProblemFromException(
            new InvalidOperationException("server fault"),
            StatusCodes.Status500InternalServerError);

        problem.Extensions.Should().ContainKey("recoverable");
        problem.Extensions["recoverable"].Should().Be(true);
    }

    [Test]
    public void FromException_503_Is_Recoverable()
    {
        var problem = GetProblemFromException(
            new InvalidOperationException("service unavailable"),
            StatusCodes.Status503ServiceUnavailable);

        problem.Extensions.Should().ContainKey("recoverable");
        problem.Extensions["recoverable"].Should().Be(true);
    }

    [Test]
    public void FromException_400_Is_Not_Recoverable()
    {
        var problem = GetProblemFromException(
            new ArgumentException("bad argument"),
            StatusCodes.Status400BadRequest);

        problem.Extensions.Should().ContainKey("recoverable");
        problem.Extensions["recoverable"].Should().Be(false);
    }

    [Test]
    public void FromException_ValidationException_Is_Not_Recoverable()
    {
        var failures = new[]
        {
            new FluentValidation.Results.ValidationFailure("Email", "invalid")
        };
        var ex = new FluentValidation.ValidationException(failures);

        var mapper = Substitute.For<IExceptionStatusCodeMapper>();
        mapper.TryMap(ex, out Arg.Any<int>())
            .Returns(call => { call[1] = StatusCodes.Status400BadRequest; return true; });

        var correlationIdService = Substitute.For<ICorrelationIdService>();
        correlationIdService.GetOrGenerate().Returns(CorrelationId.Generate());

        var filter = new ExceptionFilter(
            NullLogger<ExceptionFilter>.Instance,
            correlationIdService,
            mapper,
            Options.Create(new ApiExceptionOptions()));

        var ctx = BuildExceptionContext(ex);
        filter.OnException(ctx);

        var problem = ((ObjectResult)ctx.Result!).Value.Should().BeAssignableTo<ProblemDetails>().Subject;
        problem.Extensions.Should().ContainKey("recoverable");
        problem.Extensions["recoverable"].Should().Be(false);
    }

    // ---- helpers ----

    private static async Task<ProblemDetails> GetProblemFromResult(object resultValue)
    {
        var correlationIdService = Substitute.For<ICorrelationIdService>();
        correlationIdService.GetOrGenerate().Returns(CorrelationId.Generate());

        var filter = new ResultConversionFilter(correlationIdService);
        var context = BuildResultContext(resultValue);

        await filter.OnResultExecutionAsync(context, NoOpNext);

        var obj = context.Result.Should().BeOfType<ObjectResult>().Subject;
        return obj.Value.Should().BeAssignableTo<ProblemDetails>().Subject;
    }

    private static ProblemDetails GetProblemFromException(Exception exception, int mappedStatus)
    {
        var mapper = Substitute.For<IExceptionStatusCodeMapper>();
        mapper.TryMap(exception, out Arg.Any<int>())
            .Returns(call => { call[1] = mappedStatus; return true; });

        var correlationIdService = Substitute.For<ICorrelationIdService>();
        correlationIdService.GetOrGenerate().Returns(CorrelationId.Generate());

        var filter = new ExceptionFilter(
            NullLogger<ExceptionFilter>.Instance,
            correlationIdService,
            mapper,
            Options.Create(new ApiExceptionOptions()));

        var ctx = BuildExceptionContext(exception);
        filter.OnException(ctx);

        var obj = ctx.Result.Should().BeOfType<ObjectResult>().Subject;
        return obj.Value.Should().BeAssignableTo<ProblemDetails>().Subject;
    }

    private static ResultExecutingContext BuildResultContext(object value) =>
        new(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            new ObjectResult(value),
            controller: new object());

    private static Task<ResultExecutedContext> NoOpNext()
    {
        var ctx = new ResultExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            new EmptyResult(),
            controller: new object());
        return Task.FromResult(ctx);
    }

    private static ExceptionContext BuildExceptionContext(Exception ex) =>
        new(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>())
        {
            Exception = ex
        };
}

