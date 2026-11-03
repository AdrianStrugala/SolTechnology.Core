using System.Globalization;

namespace SolTechnology.Core.AUID;

public readonly partial struct Auid
{
    /// <summary>
    /// Format: COD_YYYYMMDDHHmmss_RANDOM
    /// Length: 3 + 1 + 14 + 1 + 6 = 25 chars
    /// Timestamp: YYYYMMDDHHmmss (readable date/time)
    /// Random: 6 decimal digits (17 bits = 0-131071)
    /// Example: CTY_20241205123456_012345
    /// </summary>
    public override string ToString()
    {
        if (Value == 0)
        {
            return $"AAA{Separator}20010101000000{Separator}000000";
        }

        return string.Create(25, Value, (span, val) =>
        {
            long codePart = val >>> (BitsTime + BitsRandom); // Use ulong shift to handle sign bit correctly
            long timePart = (val >> BitsRandom) & MaskTime;
            long randPart = val & MaskRandom;

            span[0] = (char)('A' + (codePart / 676));
            span[1] = (char)('A' + ((codePart % 676) / 26));
            span[2] = (char)('A' + (codePart % 26));

            span[3] = Separator;

            var dateTime = new DateTime(EpochTicks + (timePart * TimeSpan.TicksPerSecond), DateTimeKind.Utc);
            dateTime.TryFormat(span.Slice(4, 14), out _, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            span[18] = Separator;

            randPart.TryFormat(span.Slice(19, 6), out _, "D6", CultureInfo.InvariantCulture);
        });
    }
}
