using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using NUnit.Framework;
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

    [Test]
    public void FromDeserializationFailure_Returns_NonRecoverable_Error()
    {
        var ex = new System.Text.Json.JsonException("Unexpected char");

        var error = ServiceCallErrorMapper.FromDeserializationFailure(ex, "garbage");

        error.Message.Should().Be("Response deserialization failed");
        error.Recoverable.Should().BeFalse();
    }

    [Test]
    public async Task TryGetAsync_Success_Returns_Result_With_Data()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://test/api/data")
            .Respond("application/json", """{"name":"foo"}""");

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://test/");

        var result = await client.CreateRequest("/api/data").TryGetAsync<TestDto>();

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("foo");
    }

    [Test]
    public async Task TryGetAsync_404_Returns_NotFoundError()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://test/api/missing")
            .Respond(HttpStatusCode.NotFound, "application/json", """{"title":"Not found"}""");

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://test/");

        var result = await client.CreateRequest("/api/missing").TryGetAsync<TestDto>();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        result.Error!.Message.Should().Be("Not found");
    }

    [Test]
    public async Task TryGetAsync_BadJson_Returns_DeserializationError()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://test/api/bad")
            .Respond("application/json", "not json at all {{{");

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://test/");

        var result = await client.CreateRequest("/api/bad").TryGetAsync<TestDto>();

        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Be("Response deserialization failed");
        result.Error.Recoverable.Should().BeFalse();
    }

    private sealed record TestDto
    {
        public string Name { get; init; } = "";
    }
}

