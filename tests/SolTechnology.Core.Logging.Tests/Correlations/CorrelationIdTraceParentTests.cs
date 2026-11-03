using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.Logging.Tests.Correlations;

public class CorrelationIdTraceParentTests
{
    [TestCase(true, true, true)]
    [TestCase(true, false, false)]
    [TestCase(false, false, false)]
    public void EnrichResponse_TraceParent_OnlyWhenValueIsCurrentTraceId(
        bool activityInScope, bool idFromActivity, bool expectTraceParent)
    {
        // Arrange
        Activity.Current = null;
        using var activity = activityInScope ? new Activity("request").Start() : null;
        var sut = idFromActivity ? CorrelationId.Generate() : CorrelationId.FromString("support-ticket-42");
        var context = new DefaultHttpContext();

        // Act
        sut.EnrichResponse(context.Response);

        // Assert
        context.Response.Headers[CorrelationId.HeaderKey].ToString().Should().Be(sut.Value);
        context.Response.Headers.ContainsKey(CorrelationId.TraceParentHeaderKey).Should().Be(expectTraceParent);
        if (expectTraceParent)
        {
            context.Response.Headers[CorrelationId.TraceParentHeaderKey].ToString().Should()
                .Be($"00-{activity!.TraceId.ToHexString()}-{activity.SpanId.ToHexString()}-00");
        }
    }
}
