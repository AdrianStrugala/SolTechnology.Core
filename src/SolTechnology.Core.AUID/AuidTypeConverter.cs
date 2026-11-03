using System.ComponentModel;
using System.Globalization;

namespace SolTechnology.Core.AUID;

/// <summary>
/// Type converter for AUID, enabling automatic conversion in JSON serializers and other frameworks.
/// Converts between AUID and string representations.
/// </summary>
public sealed class AuidTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            if (Auid.TryParse(str, culture, out var auid))
            {
                return auid;
            }

            throw new FormatException($"Unable to convert [{str}] to Auid. Expected format COD_YYYYMMDDHHmmss_RANDOM.");
        }

        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is Auid auid)
        {
            return auid.ToString();
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
