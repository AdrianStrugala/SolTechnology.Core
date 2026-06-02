using DotNet.Testcontainers.Configurations;
using IContainer = DotNet.Testcontainers.Containers.IContainer;

namespace SolTechnology.Core.SQL.Testing.Engines;

/// <summary>
/// Postgres readiness wait strategy: the official image logs
/// <c>database system is ready to accept connections</c> twice (once during init, once for real),
/// so we wait for the second occurrence before declaring the container ready.
/// </summary>
internal sealed class PostgresReadinessWaitStrategy : IWaitUntil
{
    private const string TargetLogMessage = "database system is ready to accept connections";
    private const int RequiredMessageCount = 2;

    public async Task<bool> UntilAsync(IContainer container)
    {
        var logs = await container.GetLogsAsync().ConfigureAwait(false);
        var lines = logs.Stdout.Split('\n').Concat(logs.Stderr.Split('\n'));
        var count = lines.Count(line => line.Contains(TargetLogMessage, StringComparison.Ordinal));
        return count >= RequiredMessageCount;
    }
}

