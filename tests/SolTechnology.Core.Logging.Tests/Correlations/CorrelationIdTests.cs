using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using SolTechnology.Core.Logging.Correlations;
using Xunit;

namespace SolTechnology.Core.Logging.Tests.Correlations;

public class CorrelationIdTests
{
    [Fact]
    public void FromRequest_uses_X_Correlation_Id_header_when_no_activity_in_scope()
    {
        // Ensure no Activity is in scope so the header path is exercised.
        Activity.Current = null;

        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationId.HeaderKey] = "my-custom-id";

        var id = CorrelationId.FromRequest(ctx.Request, out var error);

        id.Value.Should().Be("my-custom-id");
        error.Should().BeNull();
    }

    [Fact]
    public void FromRequest_rejects_overlong_header_and_generates_new_id()
    {
        Activity.Current = null;
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationId.HeaderKey] = new string('a', CorrelationId.MaxLength + 1);

        var id = CorrelationId.FromRequest(ctx.Request, out var error);

        error.Should().Contain(CorrelationId.HeaderKey);
        id.Value.Should().NotBeNullOrWhiteSpace();
        id.Value.Length.Should().BeLessThanOrEqualTo(CorrelationId.MaxLength);
    }

    [Fact]
    public void FromRequest_prefers_activity_trace_id_over_header()
    {
        using var activity = new Activity("test").Start();
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationId.HeaderKey] = "header-id";

        var id = CorrelationId.FromRequest(ctx.Request, out var error);

        error.Should().BeNull();
        id.Value.Should().Be(activity.TraceId.ToHexString());
    }

    [Fact]
    public void EnrichResponse_sets_X_Correlation_Id()
    {
        Activity.Current = null;
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationId.HeaderKey] = "id-1";

        var id = CorrelationId.FromRequest(ctx.Request, out _);
        id.EnrichResponse(ctx.Response);

        ctx.Response.Headers[CorrelationId.HeaderKey].ToString().Should().Be("id-1");
    }

    [Fact]
    public void GetScope_returns_correlation_under_canonical_key()
    {
        Activity.Current = null;
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationId.HeaderKey] = "scope-id";

        var id = CorrelationId.FromRequest(ctx.Request, out _);
        var scope = id.GetScope();

        scope.Should().ContainKey(CorrelationId.ScopeKey).WhoseValue.Should().Be("scope-id");
    }
}

