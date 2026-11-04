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
/// Pins the <c>ProblemDetails.title</c> source (P-09): titles of mapped exceptions are fixed
/// texts, so paths, keys, parameter names, upstream URIs and query strings never reach the client.
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

    [TestCase(typeof(UnauthorizedAccessException), "Access to the path '/secret/keys.json' is denied.", StatusCodes.Status403Forbidden, "Forbidden")]
    [TestCase(typeof(KeyNotFoundException), "The given key 'tenant-42:/secret' was not present in the dictionary.", StatusCodes.Status404NotFound, "Not Found")]
    [TestCase(typeof(ArgumentException), "Path '/secret/db.sqlite' is invalid. (Parameter 'connectionString')", StatusCodes.Status400BadRequest, "Bad Request")]
    public void Create_Mapped4xx_TitleIsReasonPhrase_MessageOnlyInDiagnostics(Type exceptionType, string message, int expectedStatus, string expectedTitle)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;
        var context = BuildContext(exception);

        // Act
        NewFilter(new ApiExceptionOptions { IncludeExceptionDetails = true }).OnException(context);

        // Assert
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(expectedStatus);
        var problem = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Title.Should().Be(expectedTitle).And.NotContain("/secret");
        problem.Detail.Should().BeNull();
        var diagnostic = problem.Extensions["exception"]
            .Should().BeAssignableTo<IDictionary<string, string?>>().Subject;
        diagnostic["message"].Should().Be(message);
    }

    [Test]
    public void Create_CustomMappedUnknown4xx_TitleIsGenericRejection()
    {
        // Arrange
        var exception = new InvalidOperationException("Quota for '/secret' exceeded.");
        var mapper = Substitute.For<IExceptionStatusCodeMapper>();
        mapper.TryMap(exception, out Arg.Any<int>()).Returns(call =>
        {
            call[1] = 460;
            return true;
        });
        var context = BuildContext(exception);

        // Act
        NewFilter(new ApiExceptionOptions(), mapper).OnException(context);

        // Assert
        var problem = ((ObjectResult)context.Result!).Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Title.Should().Be("The request was rejected");
        problem.Extensions.Should().NotContainKey("exception");
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
