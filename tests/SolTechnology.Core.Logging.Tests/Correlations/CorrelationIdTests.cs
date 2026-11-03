using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using SolTechnology.Core.Logging.Correlations;
using NUnit.Framework;

namespace SolTechnology.Core.Logging.Tests.Correlations;

public class CorrelationIdTests
{
    [Test]
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

    [Test]
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

    [TestCase("abc\r\nInjected")]
    [TestCase("<script>")]
    [TestCase("id with spaces")]
    [TestCase("a,b")]
    public void FromRequest_InvalidInboundId_GeneratesNew(string inboundId)
    {
        // Arrange
        Activity.Current = null;
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationId.HeaderKey] = inboundId;

        // Act
        var id = CorrelationId.FromRequest(ctx.Request, out var error);

        // Assert
        id.Value.Should().MatchRegex("^[0-9a-f]{32}$");
        error.Should().Contain(CorrelationId.HeaderKey).And.NotContain(inboundId);
    }

    [TestCase("my-custom-id")]
    [TestCase("Order_42.retry:3")]
    public void FromRequest_AllowedInboundId_IsKept(string inboundId)
    {
        // Arrange
        Activity.Current = null;
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationId.HeaderKey] = inboundId;

        // Act
        var id = CorrelationId.FromRequest(ctx.Request, out var error);

        // Assert
        id.Value.Should().Be(inboundId);
        error.Should().BeNull();
    }

    [Test]
    public void FromRequest_ValidHeaderWithoutTraceContext_UsesHeaderOverLocalActivity()
    {
        // Arrange
        Activity.Current = null;
        using var activity = new Activity("request").Start();
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationId.HeaderKey] = "header-id";

        // Act
        var id = CorrelationId.FromRequest(ctx.Request, out var error);

        // Assert
        error.Should().BeNull();
        id.Value.Should().Be("header-id");
    }

    [Test]
    public void FromRequest_TraceParentHeader_UsesActivityTraceIdOverHeader()
    {
        // Arrange
        Activity.Current = null;
        using var activity = new Activity("request").Start();
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationId.TraceParentHeaderKey] = $"00-{activity.TraceId.ToHexString()}-b7ad6b7169203331-01";
        ctx.Request.Headers[CorrelationId.HeaderKey] = "header-id";

        // Act
        var id = CorrelationId.FromRequest(ctx.Request, out var error);

        // Assert
        error.Should().BeNull();
        id.Value.Should().Be(activity.TraceId.ToHexString());
    }

    [Test]
    public void FromRequest_ActivityWithRemoteParent_UsesActivityTraceIdOverHeader()
    {
        // Arrange
        Activity.Current = null;
        using var source = new ActivitySource(nameof(FromRequest_ActivityWithRemoteParent_UsesActivityTraceIdOverHeader));
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s == source,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        var remoteParent = new ActivityContext(
            ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded, isRemote: true);
        using var activity = source.StartActivity("request", ActivityKind.Server, remoteParent)!;
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationId.HeaderKey] = "header-id";

        // Act
        var id = CorrelationId.FromRequest(ctx.Request, out _);

        // Assert
        activity.HasRemoteParent.Should().BeTrue();
        id.Value.Should().Be(remoteParent.TraceId.ToHexString());
    }

    [TestCase(null, null)]
    [TestCase("<script>", "X-Correlation-Id")]
    public void FromRequest_NoUsableHeader_FallsBackToActivityTraceId(string? header, string? expectedError)
    {
        // Arrange
        Activity.Current = null;
        using var activity = new Activity("request").Start();
        var ctx = new DefaultHttpContext();
        if (header is not null)
        {
            ctx.Request.Headers[CorrelationId.HeaderKey] = header;
        }

        // Act
        var id = CorrelationId.FromRequest(ctx.Request, out var error);

        // Assert
        id.Value.Should().Be(activity.TraceId.ToHexString());
        if (expectedError is null)
        {
            error.Should().BeNull();
        }
        else
        {
            error.Should().Contain(expectedError);
        }
    }

    [Test]
    public void EnrichResponse_sets_X_Correlation_Id()
    {
        Activity.Current = null;
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationId.HeaderKey] = "id-1";

        var id = CorrelationId.FromRequest(ctx.Request, out _);
        id.EnrichResponse(ctx.Response);

        ctx.Response.Headers[CorrelationId.HeaderKey].ToString().Should().Be("id-1");
    }

    [Test]
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

