using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Logging.Tests.Middleware;

/// <summary>Real recorders from <c>AddSolChronicle</c> for middleware tests that call <c>InvokeAsync</c> directly.</summary>
internal static class TestChronicles
{
    internal static IChronicle Create(bool enabled = true)
        => new ServiceCollection()
            .AddLogging()
            .AddSolChronicle(o => o.Enabled = enabled)
            .BuildServiceProvider()
            .GetRequiredService<IChronicle>();
}
