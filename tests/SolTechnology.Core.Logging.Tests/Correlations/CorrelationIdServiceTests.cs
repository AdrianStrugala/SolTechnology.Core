using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Logging.Correlations;
using NUnit.Framework;

namespace SolTechnology.Core.Logging.Tests.Correlations;

public class CorrelationIdServiceTests
{
    private static ICorrelationIdService BuildService()
    {
        var services = new ServiceCollection();
        services.AddCoreLogging();
        return services.BuildServiceProvider().GetRequiredService<ICorrelationIdService>();
    }

    [Test]
    public async Task Set_and_Get_flow_through_async_local()
    {
        var service = BuildService();
        var id = CorrelationId.Generate();
        service.Set(id);

        await Task.Yield();

        service.Get().Should().Be(id);
    }

    [Test]
    public void GetOrGenerate_creates_value_when_none_set()
    {
        var service = BuildService();
        service.Get().Should().BeNull();

        var first = service.GetOrGenerate();
        var second = service.GetOrGenerate();

        first.Should().NotBeNull();
        first.Should().BeSameAs(second);
    }
}


