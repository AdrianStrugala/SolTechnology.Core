using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using NUnit.Framework;
using Polly.CircuitBreaker;
using Polly.Timeout;
using RichardSzalay.MockHttp;
using SolTechnology.Core;
using SolTechnology.Core.Errors;
using SolTechnology.Core.HTTP;

namespace SolTechnology.Core.HTTP.Tests;

/// <summary>
/// Verifies <see cref="ServiceCallErrorMapper"/> and the <c>TryXxxAsync</c> Result-returning
/// surface on <see cref="RequestBuilder"/>.
/// </summary>
public sealed class ServiceCallErrorTaxonomyTests
{
    [Test]
    public void FromStatusCode_NotFound_Returns_NotFoundError()
    {
        var error = ServiceCallErrorMapper.FromStatusCode(HttpStatusCode.NotFound, null);

        error.Should().BeOfType<NotFoundError>();
        error.Recoverable.Should().BeFalse(); // 4xx heuristic
    }

    [Test]
    public void FromStatusCode_Conflict_Returns_ConflictError()
    {
        var error = ServiceCallErrorMapper.FromStatusCode(HttpStatusCode.Conflict, null);

        error.Should().BeOfType<ConflictError>();
    }

    [Test]
    public void FromStatusCode_Unauthorized_Returns_UnauthorizedError()
    {
        var error = ServiceCallErrorMapper.FromStatusCode(HttpStatusCode.Unauthorized, null);

        error.Should().BeOfType<UnauthorizedError>();
        error.Recoverable.Should().BeFalse();
    }

    [Test]
    public void FromStatusCode_BadRequest_Returns_ValidationError()
    {
        var error = ServiceCallErrorMapper.FromStatusCode(HttpStatusCode.BadRequest, null);

        error.Should().BeOfType<ValidationError>();
        error.Recoverable.Should().BeFalse();
    }

    [Test]
    public void FromStatusCode_500_Returns_Recoverable_Error()
    {
        var error = ServiceCallErrorMapper.FromStatusCode(
            HttpStatusCode.InternalServerError, null);

        error.Recoverable.Should().BeTrue();
    }

    [Test]
    public void FromStatusCode_500_With_RecoverableFalse_Overrides_Heuristic()
    {
        var error = ServiceCallErrorMapper.FromStatusCode(
            HttpStatusCode.InternalServerError,
            """{"title":"Payment rejected","recoverable":false}""");

        error.Recoverable.Should().BeFalse();
        error.Message.Should().Be("Payment rejected");
    }

    [Test]
    public void FromStatusCode_400_With_RecoverableTrue_Overrides_Heuristic()
    {
        var error = ServiceCallErrorMapper.FromStatusCode(
            HttpStatusCode.BadRequest,
            """{"title":"Rate limited","recoverable":true}""");

        error.Recoverable.Should().BeTrue();
    }

    [TestCase("[1, 2]")]
    [TestCase("\"plain text\"")]
    [TestCase("{\"title\": 5, \"recoverable\": \"yes\"}")]
    [TestCase("not json {{{")]
    public void FromStatusCode_BodyWithoutUsableProblemFields_FallsBackToStatus(string body)
    {
        // Act
        var error = ServiceCallErrorMapper.FromStatusCode(HttpStatusCode.InternalServerError, body);

        // Assert
        error.Message.Should().Be("HTTP 500 InternalServerError");
        error.Recoverable.Should().BeTrue();
    }

    [Test]
    public void FromStatusCode_GatewayTimeout_Returns_TimeoutError()
    {
        var error = ServiceCallErrorMapper.FromStatusCode(HttpStatusCode.GatewayTimeout, null);

        error.Should().BeOfType<TimeoutError>();
        error.Recoverable.Should().BeTrue();
    }

    [Test]
    public void FromException_SocketException_Returns_Recoverable_TimeoutError()
    {
        var ex = new HttpRequestException("conn refused",
            new SocketException((int)SocketError.ConnectionRefused));

        var error = ServiceCallErrorMapper.FromException(ex);

        error.Should().BeOfType<TimeoutError>();
        error.Recoverable.Should().BeTrue();
        error.Message.Should().Be("Connection failed");
    }

    private static IEnumerable<Exception> OpenCircuitExceptions()
    {
        yield return new BrokenCircuitException("The circuit is now open");
        yield return new IsolatedCircuitException("The circuit is manually held open");
    }

    [TestCaseSource(nameof(OpenCircuitExceptions))]
    public void FromException_OpenCircuit_Returns_Recoverable_UpstreamCircuitOpen(Exception exception)
    {
        // Act
        var error = ServiceCallErrorMapper.FromException(exception);

        // Assert
        error.Message.Should().Be("Upstream circuit open");
        error.Description.Should().Be(exception.Message);
        error.Recoverable.Should().BeTrue();
    }

    [Test]
    public void FromException_TimeoutRejected_StaysTimeoutError()
    {
        // Act
        var error = ServiceCallErrorMapper.FromException(new TimeoutRejectedException("timed out"));

        // Assert
        error.Should().BeOfType<TimeoutError>();
        error.Message.Should().Be("Service call timed out");
        error.Recoverable.Should().BeTrue();
    }

    [Test]
    public void FromDeserializationFailure_Returns_NonRecoverable_Error()
    {
        var ex = new System.Text.Json.JsonException("Unexpected char");

        var error = ServiceCallErrorMapper.FromDeserializationFailure(ex, "garbage");

        error.Message.Should().Be("Response deserialization failed");
        error.Recoverable.Should().BeFalse();
    }

    [Test]
    public async Task TryGet_Success_Returns_Result_With_Data()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://test/api/data")
            .Respond("application/json", """{"name":"foo"}""");

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://test/");

        var result = await client.CreateRequest("/api/data").TryGet<TestDto>();

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("foo");
    }

    [Test]
    public async Task TryGet_404_Returns_NotFoundError()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://test/api/missing")
            .Respond(HttpStatusCode.NotFound, "application/json", """{"title":"Not found"}""");

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://test/");

        var result = await client.CreateRequest("/api/missing").TryGet<TestDto>();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        result.Error!.Message.Should().Be("Not found");
    }

    [Test]
    public async Task TryGet_BadJson_Returns_DeserializationError()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://test/api/bad")
            .Respond("application/json", "not json at all {{{");

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://test/");

        var result = await client.CreateRequest("/api/bad").TryGet<TestDto>();

        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Be("Response deserialization failed");
        result.Error.Recoverable.Should().BeFalse();
    }

    private sealed record TestDto
    {
        public string Name { get; init; } = "";
    }
}

