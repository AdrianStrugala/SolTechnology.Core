using System.Net;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using SolTechnology.Core.API.Exceptions;
using NUnit.Framework;

namespace SolTechnology.Core.API.Tests;

/// <summary>
/// The default mapper is opinionated by design (premortem #2/#3/#4). These tests pin the
/// behaviour so an accidental edit changes the wire contract loudly, not silently — every
/// unintentional flip would otherwise show up as misclassified 4xx/5xx in production SLO.
/// </summary>
public sealed class DefaultExceptionStatusCodeMapperTests
{
    private readonly DefaultExceptionStatusCodeMapper _mapper = new();

    [Test]
    public void ValidationException_Maps_To_400()
    {
        var failure = new ValidationFailure("Email", "invalid");
        var ex = new ValidationException(new[] { failure });

        var mapped = _mapper.TryMap(ex, out var status);

        mapped.Should().BeTrue();
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Test]
    [TestCase(typeof(ArgumentException))]
    [TestCase(typeof(ArgumentNullException))]
    public void ArgumentException_And_Subtypes_Map_To_400(Type exceptionType)
    {
        var ex = (Exception)Activator.CreateInstance(exceptionType)!;

        _mapper.TryMap(ex, out var status).Should().BeTrue();
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Test]
    public void UnauthorizedAccessException_Maps_To_403_Not_401()
    {
        // Documented decision (RFC 7235): 401 means "no credentials", emitted by the auth
        // pipeline before MVC runs. UnauthorizedAccessException means "identified, forbidden".
        _mapper.TryMap(new UnauthorizedAccessException(), out var status).Should().BeTrue();
        status.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Test]
    public void KeyNotFoundException_Maps_To_404()
    {
        _mapper.TryMap(new KeyNotFoundException(), out var status).Should().BeTrue();
        status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public void NotImplementedException_Maps_To_501()
    {
        _mapper.TryMap(new NotImplementedException(), out var status).Should().BeTrue();
        status.Should().Be(StatusCodes.Status501NotImplemented);
    }

    [Test]
    public void HttpRequestException_WithoutStatus_Maps_To_502_BadGateway()
    {
        _mapper.TryMap(new HttpRequestException("boom"), out var status).Should().BeTrue();
        status.Should().Be(StatusCodes.Status502BadGateway);
    }

    [Test]
    [TestCase(HttpStatusCode.RequestTimeout)]
    [TestCase(HttpStatusCode.GatewayTimeout)]
    public void HttpRequestException_WithTimeoutStatus_Maps_To_504(HttpStatusCode upstream)
    {
        var ex = new HttpRequestException("upstream timed out", inner: null, statusCode: upstream);

        _mapper.TryMap(ex, out var status).Should().BeTrue();
        status.Should().Be(StatusCodes.Status504GatewayTimeout);
    }

    [Test]
    public void UnknownExceptionType_Returns_False_So_Filter_Rethrows()
    {
        // A+E policy: unmapped types must NOT receive a default status. The filter relies on
        // TryMap returning false to LogCritical + rethrow. A "helpful" default 500 here would
        // remove the only signal we have that the exception type was never reviewed.
        _mapper.TryMap(new InvalidOperationException("server bug"), out var status).Should().BeFalse();
        status.Should().Be(0);
    }

    [Test]
    public void Subclass_Can_Extend_Map_Without_Losing_Defaults()
    {
        // The interface contract advertises subclass + base.TryMap as the extension point.
        // A consumer adding a domain rule must not lose ValidationException→400.
        var custom = new CustomMapper();

        custom.TryMap(new MyDomainTimeout(), out var customStatus).Should().BeTrue();
        customStatus.Should().Be(StatusCodes.Status504GatewayTimeout);

        custom.TryMap(new ValidationException("x"), out var defaultStatus).Should().BeTrue();
        defaultStatus.Should().Be(StatusCodes.Status400BadRequest);
    }

    private sealed class MyDomainTimeout : Exception;

    private sealed class CustomMapper : DefaultExceptionStatusCodeMapper
    {
        public override bool TryMap(Exception exception, out int statusCode)
        {
            if (exception is MyDomainTimeout)
            {
                statusCode = StatusCodes.Status504GatewayTimeout;
                return true;
            }
            return base.TryMap(exception, out statusCode);
        }
    }
}


