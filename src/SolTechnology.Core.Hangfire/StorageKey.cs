using System.Security.Cryptography;
using System.Text;

namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Keeps recurring job ids and storage keys within the Hangfire.SqlServer key columns (<c>nvarchar(100)</c>):
/// a value that fits is used as is; a longer one becomes a readable prefix, <c>-</c>, and the first 12 hex
/// characters of the SHA-256 of the full value, which is the same in every process and version.
/// </summary>
internal static class StorageKey
{
    /// <summary>Hangfire.SqlServer stores <c>recurring-job:&lt;id&gt;</c> in an <c>nvarchar(100)</c> key.</summary>
    public const int MaxRecurringJobIdLength = 86;

    public const int MaxKeyLength = 100;

    private const int HashLength = 12;

    public static string RecurringJobId(Type jobType) =>
        Fit(JobTypeName.Of(jobType), jobType.Name, MaxRecurringJobIdLength);

    public static string Fit(string value, string readable, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..HashLength];
        var prefixLength = Math.Min(readable.Length, maxLength - HashLength - 1);
        return $"{readable[..prefixLength]}-{hash}";
    }
}
