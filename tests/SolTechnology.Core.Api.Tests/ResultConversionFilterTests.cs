using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using SolTechnology.Core.API.Filters;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.Errors;
using SolTechnology.Core.Logging.Correlations;
using Xunit;

namespace SolTechnology.Core.API.Tests;

/// <summary>
/// Premortem scenario #1 (highest risk): a controller returning <c>Result&lt;T&gt;.Success(data)</c>
/// must produce HTTP 200 with the raw <c>data</c> in the body — not 204, and not an envelope.
/// The previous reflection-based implementation could silently drop the payload under AOT/trim;
/// these tests would have caught that regression.
/// </summary>
public sealed class ResultConversionFilterTests
{
    private record Payload(string Name, int Count);

    private readonly ICorrelationIdService _correlationIdService = Substitute.For<ICorrelationIdService>();
    private readonly ResultConversionFilter _filter;

    public ResultConversionFilterTests()
    {
        _correlationIdService.GetOrGenerate().Returns(CorrelationId.Generate());
        _filter = new ResultConversionFilter(_correlationIdService);
    }

    [Fact]
    public async Task GenericResultSuccess_Unwraps_To_RawData_With_Status200()
    {
        // Regression guard for premortem #1: AOT/trim could turn a successful Result<T> into
        // 204 No Content by losing the Data property. We assert the raw payload survives.
        var payload = new Payload("trip-42", 7);
        var context = BuildResultExecutingContext(Result<Payload>.Success(payload));

        await _filter.OnResultExecutionAsync(context, NoOpNext);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().BeSameAs(payload, "the wire body is the raw DTO, not an envelope");
    }

    [Fact]
    public async Task NonGenericResultSuccess_Maps_To_204_NoContent()
    {
        var context = BuildResultExecutingContext(Result.Success());

        await _filter.OnResultExecutionAsync(context, NoOpNext);

        context.Result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task GenericResultSuccess_With_NullData_Maps_To_204_NoContent()
    {
        // Documented edge case (Result.GetData remarks): a null payload is treated as "no body".
        // Callers needing an explicit null body must send a sentinel; default(T?) returns 204.
        var context = BuildResultExecutingContext(Result<Payload?>.Success(null));

        await _filter.OnResultExecutionAsync(context, NoOpNext);

        context.Result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Theory]
    [InlineData(typeof(NotFoundError), StatusCodes.Status404NotFound)]
    [InlineData(typeof(ConflictError), StatusCodes.Status409Conflict)]
    [InlineData(typeof(UnauthorizedError), StatusCodes.Status401Unauthorized)]
    [InlineData(typeof(ForbiddenError), StatusCodes.Status403Forbidden)]
    [InlineData(typeof(Error), StatusCodes.Status500InternalServerError)]
    public async Task GenericResultFail_Maps_ErrorSubtype_To_ProblemDetails_WithStatus(
        Type errorType, int expectedStatus)
    {
        var error = (Error)Activator.CreateInstance(errorType)!;
        error.Message = "boom";
        var context = BuildResultExecutingContext(Result<Payload>.Fail(error));

        await _filter.OnResultExecutionAsync(context, NoOpNext);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(expectedStatus);
        result.ContentTypes.Should().Contain("application/problem+json");
        result.Value.Should().BeAssignableTo<ProblemDetails>()
            .Which.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task ValidationError_Produces_ValidationProblemDetails_With_PerField_Errors()
    {
        var error = new ValidationError
        {
            Message = "Invalid input.",
            Errors = new Dictionary<string, string[]>
            {
                ["email"] = ["'Email' is not a valid email address."],
                ["age"] = ["'Age' must be greater than 0."]
            }
        };
        var context = BuildResultExecutingContext(Result<Payload>.Fail(error));

        await _filter.OnResultExecutionAsync(context, NoOpNext);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problem = result.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey("email");
        problem.Errors.Should().ContainKey("age");
    }

    [Fact]
    public async Task BareError_Returned_From_Action_Is_Converted_To_ProblemDetails()
    {
        // BadRequest(error) shorthand: action sets ObjectResult.Value = Error directly,
        // without Result wrapping. The filter still must produce ProblemDetails.
        var error = new NotFoundError { Message = "Trip 42 not found." };
        var context = BuildResultExecutingContext(error);

        await _filter.OnResultExecutionAsync(context, NoOpNext);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        result.Value.Should().BeAssignableTo<ProblemDetails>();
    }

    [Fact]
    public async Task NonResultPayload_Is_Passed_Through_Untouched()
    {
        // DTO returned directly from controller — filter must not touch it.
        var payload = new Payload("trip-42", 7);
        var context = BuildResultExecutingContext(payload);

        await _filter.OnResultExecutionAsync(context, NoOpNext);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.Value.Should().BeSameAs(payload);
        result.StatusCode.Should().BeNull("filter must not override status for non-Result values");
    }

    [Fact]
    public async Task Failed_Result_Without_Error_Falls_Back_To_500_ProblemDetails()
    {
        // Malformed Result (IsSuccess=false but Error=null) — must still produce a valid body,
        // not a NullReferenceException at the boundary.
        var malformed = new Result<Payload> { IsSuccess = false, Error = null };
        var context = BuildResultExecutingContext(malformed);

        await _filter.OnResultExecutionAsync(context, NoOpNext);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Value.Should().BeAssignableTo<ProblemDetails>();
    }

    [Fact]
    public async Task Error_CorrelationId_Is_Preserved_When_Already_Set()
    {
        // Source-of-truth contract: if the error already carries a correlation id (e.g. set by
        // a CQRS pipeline behavior), the filter must echo it, not regenerate.
        var error = new NotFoundError { Message = "x", CorrelationId = "abc-123" };
        var context = BuildResultExecutingContext(Result<Payload>.Fail(error));

        await _filter.OnResultExecutionAsync(context, NoOpNext);

        var problem = ((ObjectResult)context.Result!).Value.Should().BeAssignableTo<ProblemDetails>().Subject;
        problem.Extensions.Should().ContainKey("correlationId");
        problem.Extensions["correlationId"].Should().Be("abc-123");
        _correlationIdService.DidNotReceive().GetOrGenerate();
    }

    // ---- helpers ---------------------------------------------------------

    private static Task NoOpNext()
    {
        var ctx = new ResultExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            new EmptyResult(),
            controller: new object());
        return Task.FromResult(ctx);
    }

    private static ResultExecutingContext BuildResultExecutingContext(object value)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new ObjectResult(value),
            controller: new object());
    }
}


