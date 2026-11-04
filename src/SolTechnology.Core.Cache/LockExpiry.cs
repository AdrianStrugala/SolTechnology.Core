namespace SolTechnology.Core.Cache;

/// <summary>The expiry rule shared by every <see cref="IDistributedLockService"/>: greater than zero, at most 365 days.</summary>
internal static class LockExpiry
{
    public static readonly TimeSpan Maximum = TimeSpan.FromDays(365);

    public static void Validate(TimeSpan expiry)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiry, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(expiry, Maximum);
    }
}
