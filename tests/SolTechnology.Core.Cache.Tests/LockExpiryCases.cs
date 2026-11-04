namespace SolTechnology.Core.Cache.Tests;

internal static class LockExpiryCases
{
    public static readonly TimeSpan Maximum = TimeSpan.FromDays(365);

    public static IEnumerable<TimeSpan> OutOfRange =>
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(-1),
        Maximum + TimeSpan.FromTicks(1),
        TimeSpan.MaxValue
    ];
}
