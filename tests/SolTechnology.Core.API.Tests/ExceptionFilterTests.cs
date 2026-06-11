using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
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
using SolTechnology.Core.Logging.Correlations;
using NUnit.Framework;

namespace SolTechnology.Core.API.Tests;

/// <summary>
/// Pins the exception filter contract that protects production: no stack trace by default
/// (CWE-209), unmapped types are rethrown (A+E policy), mapped exceptions produce RFC 7807
/// bodies with correlation id, validation failures produce per-field <c>errors</c>.
/// </summary>
public sealed class ExceptionFilterTests
{
    private readonly IExceptionStatusCodeMapper _mapper = Substitute.For<IExceptionStatusCodeMapper>();
    private readonly ICorrelationIdService _correlationIdService = Substitute.For<ICorrelationIdService>();
    private readonly ApiExceptionOptions _options = new();

    public ExceptionFilterTests()
    {
        _correlationIdService.GetOrGenerate().Returns(CorrelationId.Generate());
    }

    [Test]
    public void MappedException_Produces_ProblemDetails_With_CorrelationId_And_NoStackTrace_ByDefault()
    {
        // CWE-209 guard: IncludeExceptionDetails defaults to false. Stack trace must not leak
        // to the wire even though the exception object reaches the factory.
        var statusCode = StatusCodes.Status404NotFound;
        _mapper.TryMap(Arg.Any<Exception>(), out Arg.Any<int>())
            .Returns(call => { call[1] = statusCode; return true; });

        var filter = NewFilter();
        var ctx = BuildContext(new KeyNotFoundException("trip 42"));

        filter.OnException(ctx);

        ctx.ExceptionHandled.Should().BeTrue();
        var result = ctx.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(statusCode);
        result.ContentTypes.Should().Contain("application/problem+json");
        var problem = result.Value.Should().BeAssignableTo<ProblemDetails>().Subject;
        problem.Extensions.Should().ContainKey("correlationId");
        problem.Extensions.Should().NotContainKey("exception",
            "stack trace must not leak in Production — IncludeExceptionDetails is false by default");
    }

    [Test]
    public void MappedException_With_IncludeExceptionDetails_Emits_Diagnostic_Block()
    {
        _options.IncludeExceptionDetails = true;
        _mapper.TryMap(Arg.Any<Exception>(), out Arg.Any<int>())
            .Returns(call => { call[1] = StatusCodes.Status500InternalServerError; return true; });

        var filter = NewFilter();
        var ctx = BuildContext(new InvalidOperationException("boom"));

        filter.OnException(ctx);

        var problem = ((ObjectResult)ctx.Result!).Value.Should().BeAssignableTo<ProblemDetails>().Subject;
        problem.Extensions.Should().ContainKey("exception",
            "developer opted in to diagnostic detail explicitly");
    }

    [Test]
    public void UnmappedException_Is_Rethrown_To_Host_Via_ExceptionHandledFalse()
    {
        // A+E policy: the filter does not invent a default status. ExceptionHandled stays
        // false so MVC rethrows to the host pipeline (DeveloperExceptionPage / UseExceptionHandler).
        _mapper.TryMap(Arg.Any<Exception>(), out Arg.Any<int>()).Returns(false);

        var filter = NewFilter();
        var ctx = BuildContext(new InvalidOperationException("unknown server fault"));

        filter.OnException(ctx);

        ctx.ExceptionHandled.Should().BeFalse();
        ctx.Result.Should().BeNull();
    }

    [Test]
    public void ClientAbort_Is_Silently_Skipped()
    {
        // When the client cancels mid-request, we don't try to write a body. ExceptionHandled
        // stays false; MVC swallows the OperationCanceledException by convention. Logging
        // middleware downgrades the finish event.
        _mapper.TryMap(Arg.Any<Exception>(), out Arg.Any<int>()).Returns(false);

        var filter = NewFilter();
        var httpContext = new DefaultHttpContext();
        httpContext.RequestAborted = new CancellationToken(canceled: true);
        var ctx = BuildContext(new OperationCanceledException(), httpContext);

        filter.OnException(ctx);

        ctx.ExceptionHandled.Should().BeFalse();
        ctx.Result.Should().BeNull();
    }

    [Test]
    public void ValidationException_Becomes_ValidationProblemDetails_With_PerField_Errors()
    {
        // The factory's special-case branch: FluentValidation failures are grouped by property
        // and surfaced through ValidationProblemDetails.errors so clients can render per-field
        // messages without parsing a stringified blob.
        var failures = new[]
        {
            new ValidationFailure("Email", "'Email' is not a valid email address."),
            new ValidationFailure("Age", "'Age' must be greater than 0."),
            new ValidationFailure("Email", "must be unique"),
        };
        var ex = new ValidationException(failures);
        _mapper.TryMap(ex, out Arg.Any<int>())
            .Returns(call => { call[1] = StatusCodes.Status400BadRequest; return true; });

        var filter = NewFilter();
        var ctx = BuildContext(ex);

        filter.OnException(ctx);

        var problem = ((ObjectResult)ctx.Result!).Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey("Email");
        problem.Errors["Email"].Should().HaveCount(2, "multiple failures on the same field collect together");
        problem.Errors.Should().ContainKey("Age");
    }

    // ---- helpers ---------------------------------------------------------

    private ExceptionFilter NewFilter() => new(
        NullLogger<ExceptionFilter>.Instance,
        _correlationIdService,
        _mapper,
        Options.Create(_options));

    private static ExceptionContext BuildContext(Exception exception, HttpContext? httpContext = null)
    {
        var actionContext = new ActionContext(
            httpContext ?? new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
    }
}


