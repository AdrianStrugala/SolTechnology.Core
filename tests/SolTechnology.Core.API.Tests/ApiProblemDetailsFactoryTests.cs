using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.API.Exceptions;
using SolTechnology.Core.API.Filters;
using SolTechnology.Core.HTTP;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.API.Tests;

/// <summary>
/// Pins the <c>ProblemDetails.title</c> source (P-09): 5xx titles are fixed texts so upstream
/// URIs and query strings never reach the client; 4xx titles keep the exception message.
/// </summary>
[TestFixture]
public sealed class ApiProblemDetailsFactoryTests
{
    private const string Secret = "SECRET";
    private static readonly Uri UpstreamUri = new($"https://maps.example/geocode?address=Paris&key={Secret}");

    [TestCase(HttpStatusCode.BadGateway, StatusCodes.Status502BadGateway, "Upstream dependency failed")]
    [TestCase(HttpStatusCode.GatewayTimeout, StatusCodes.Status504GatewayTimeout, "Upstream dependency timed out")]
    public void Create_HttpRequestFailed502_TitleHasNoUri(HttpStatusCode upstreamStatus, int expectedStatus, string expectedTitle)
    {
        // Arrange
        var exception = new HttpRequestFailedException(HttpMethod.Get, UpstreamUri, upstreamStatus, "Bad", responseBody: null);
        var context = BuildContext(exception);

        // Act
        NewFilter(new ApiExceptionOptions { IncludeExceptionDetails = true }).OnException(context);

        // Assert
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(expectedStatus);
        var problem = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Title.Should().Be(expectedTitle);
        problem.Detail.Should().BeNull();
        var diagnostic = problem.Extensions["exception"]
            .Should().BeAssignableTo<IDictionary<string, string?>>().Subject;
        diagnostic["message"].Should().NotContain(Secret).And.NotContain("?");
    }

    [TestCase(typeof(InvalidOperationException), "An unexpected error occurred")]
    [TestCase(typeof(NotImplementedException), "An unexpected error occurred")]
    public void Create_Other5xx_TitleIsGeneric(Type exceptionType, string expectedTitle)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, $"failed calling {UpstreamUri}")!;
        var mapper = Substitute.For<IExceptionStatusCodeMapper>();
        mapper.TryMap(exception, out Arg.Any<int>()).Returns(call =>
        {
            call[1] = exception is NotImplementedException ? StatusCodes.Status501NotImplemented : StatusCodes.Status500InternalServerError;
            return true;
        });
        var context = BuildContext(exception);

        // Act
        NewFilter(new ApiExceptionOptions(), mapper).OnException(context);

        // Assert
        var problem = ((ObjectResult)context.Result!).Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Title.Should().Be(expectedTitle);
        problem.Title.Should().NotContain(Secret);
    }

    [Test]
    public void Create_ValidationException_TitleIsMessage()
    {
        // Arrange
        var exception = new ArgumentException("Trip name must not be empty.");
        var context = BuildContext(exception);

        // Act
        NewFilter(new ApiExceptionOptions()).OnException(context);

        // Assert
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.Value.Should().BeOfType<ProblemDetails>().Which.Title.Should().Be("Trip name must not be empty.");
    }

    private static ExceptionFilter NewFilter(ApiExceptionOptions options, IExceptionStatusCodeMapper? mapper = null)
    {
        var correlationIdService = Substitute.For<ICorrelationIdService>();
        correlationIdService.GetOrGenerate().Returns(CorrelationId.Generate());

        return new ExceptionFilter(
            NullLogger<ExceptionFilter>.Instance,
            correlationIdService,
            mapper ?? new DefaultExceptionStatusCodeMapper(),
            Options.Create(options));
    }

    private static ExceptionContext BuildContext(Exception exception)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        return new ExceptionContext(actionContext, new List<IFilterMetadata>()) { Exception = exception };
    }
}
