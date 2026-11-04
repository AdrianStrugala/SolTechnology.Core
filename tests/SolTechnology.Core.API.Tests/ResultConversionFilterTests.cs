using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using SolTechnology.Core.API.Filters;
using SolTechnology.Core;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Logging.Correlations;
using NUnit.Framework;

namespace SolTechnology.Core.API.Tests;

/// <summary>
/// Premortem scenario #1 (highest risk): a controller returning <c>Result&lt;T&gt;.Success(data)</c>
/// must produce HTTP 200 with the raw <c>data</c> in the body — not 204, and not an envelope.
/// The previous reflection-based implementation could silently drop the payload under AOT/trim;
/// these tests would have caught that regression.
/// </summary>
[TestFixture]
public sealed class ResultConversionFilterTests
{
    private record Payload(string Name, int Count);

    // Derived types are the only way left to build a failed result without an error.
    private sealed record MalformedResult : Result<Payload>;

    private ICorrelationIdService _correlationIdService = null!;
    private ResultConversionFilter _filter = null!;

    [SetUp]
    public void SetUp()
    {
        _correlationIdService = Substitute.For<ICorrelationIdService>();
        _correlationIdService.GetOrGenerate().Returns(CorrelationId.Generate());
        _filter = new ResultConversionFilter(_correlationIdService);
    }

    [Test]
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

    [Test]
    public async Task NonGenericResultSuccess_Maps_To_204_NoContent()
    {
        var context = BuildResultExecutingContext(Result.Success());

        await _filter.OnResultExecutionAsync(context, NoOpNext);

        context.Result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Test]
    public async Task GenericResultSuccess_With_NullData_Maps_To_204_NoContent()
    {
        // Documented edge case (Result.GetData remarks): a null payload is treated as "no body".
        // Callers needing an explicit null body must send a sentinel; default(T?) returns 204.
        var context = BuildResultExecutingContext(Result<Payload?>.Success(null));

        await _filter.OnResultExecutionAsync(context, NoOpNext);

        context.Result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Test]
    [TestCase(typeof(NotFoundError), StatusCodes.Status404NotFound)]
    [TestCase(typeof(ConflictError), StatusCodes.Status409Conflict)]
    [TestCase(typeof(UnauthorizedError), StatusCodes.Status401Unauthorized)]
    [TestCase(typeof(ForbiddenError), StatusCodes.Status403Forbidden)]
    [TestCase(typeof(Error), StatusCodes.Status500InternalServerError)]
    public async Task GenericResultFail_Maps_ErrorSubtype_To_ProblemDetails_WithStatus(
        Type errorType, int expectedStatus)
    {
        var error = (Error)Activator.CreateInstance(errorType)!;
        error = error with { Message = "boom" };
        var context = BuildResultExecutingContext(Result<Payload>.Fail(error));

        await _filter.OnResultExecutionAsync(context, NoOpNext);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(expectedStatus);
        result.ContentTypes.Should().Contain("application/problem+json");
        result.Value.Should().BeAssignableTo<ProblemDetails>()
            .Which.Status.Should().Be(expectedStatus);
    }

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
    public async Task Failed_Result_Without_Error_Falls_Back_To_500_ProblemDetails()
    {
        // Malformed Result (IsSuccess=false but Error=null), e.g. from a consumer-derived result type —
        // must still produce a valid body, not a NullReferenceException at the boundary.
        var malformed = new MalformedResult();
        var context = BuildResultExecutingContext(malformed);

        await _filter.OnResultExecutionAsync(context, NoOpNext);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Value.Should().BeAssignableTo<ProblemDetails>();
    }

    [Test]
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

    [Test]
    public async Task CreatedAtAction_WithSuccessfulResult_Keeps201AndLocation()
    {
        // Arrange
        var payload = new Payload("trip-42", 7);
        var created = new CreatedAtActionResult("Get", "Trips", new { id = 42 }, Result<Payload>.Success(payload));
        var context = BuildResultExecutingContext(created);

        // Act
        await _filter.OnResultExecutionAsync(context, NoOpNext);

        // Assert
        var result = context.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Value.Should().BeSameAs(payload);
        result.ActionName.Should().Be("Get");
        result.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(42);
    }

    [Test]
    public async Task GenericResultSuccess_FromActionResultOfT_SerializesPayloadType()
    {
        // Arrange
        var payload = new Payload("trip-42", 7);
        var declared = new ObjectResult(Result<Payload>.Success(payload)) { DeclaredType = typeof(Result<Payload>) };
        var context = BuildResultExecutingContext(declared);

        // Act
        await _filter.OnResultExecutionAsync(context, NoOpNext);

        // Assert
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().BeSameAs(payload);
        result.DeclaredType.Should().BeNull("a declared Result<T> type would make the formatter reject the payload");
    }

    [Test]
    public async Task Accepted_WithSuccessfulResult_Keeps202()
    {
        // Arrange
        var payload = new Payload("trip-42", 7);
        var accepted = new AcceptedResult("/api/trips/42/status", Result<Payload>.Success(payload));
        var context = BuildResultExecutingContext(accepted);

        // Act
        await _filter.OnResultExecutionAsync(context, NoOpNext);

        // Assert
        var result = context.Result.Should().BeOfType<AcceptedResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status202Accepted);
        result.Location.Should().Be("/api/trips/42/status");
        result.Value.Should().BeSameAs(payload);
    }

    [Test]
    public async Task Ok_WithFailedResult_MapsToProblemDetails()
    {
        // Arrange
        var ok = new OkObjectResult(Result<Payload>.Fail(new NotFoundError { Message = "Trip 42 not found." }));
        var context = BuildResultExecutingContext(ok);

        // Act
        await _filter.OnResultExecutionAsync(context, NoOpNext);

        // Assert
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        result.ContentTypes.Should().Contain("application/problem+json");
        result.Value.Should().BeAssignableTo<ProblemDetails>().Which.Title.Should().Be("Trip 42 not found.");
    }

    // ---- helpers ---------------------------------------------------------

    private static Task<ResultExecutedContext> NoOpNext()
    {
        var ctx = new ResultExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            new EmptyResult(),
            controller: new object());
        return Task.FromResult(ctx);
    }

    private static ResultExecutingContext BuildResultExecutingContext(object value) =>
        BuildResultExecutingContext(new ObjectResult(value));

    private static ResultExecutingContext BuildResultExecutingContext(ObjectResult objectResult)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            objectResult,
            controller: new object());
    }
}


