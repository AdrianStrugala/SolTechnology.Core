using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace SolTechnology.Core.HTTP;

/// <summary>
/// Projects an in-memory <see cref="HttpPolicyConfiguration"/> into an <see cref="IConfiguration"/>,
/// so the explicit-parameter path binds through the same <c>ConfigurationBinder</c> as appsettings.
/// Only primitives, <see cref="string"/>, enums and <see cref="IFormattable"/> project; any other
/// property type throws instead of being silently dropped by the binder.
/// </summary>
internal static class PolicyConfigurationProjector
{
    public static IConfiguration ToConfiguration(HttpPolicyConfiguration source)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in typeof(HttpPolicyConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Delegates such as RetryPredicate are copied separately; they cannot be bound.
            if (!prop.CanRead || !prop.CanWrite || typeof(Delegate).IsAssignableFrom(prop.PropertyType))
            {
                continue;
            }

            var underlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            if (underlying != typeof(string)
                && !underlying.IsPrimitive
                && !underlying.IsEnum
                && !typeof(IFormattable).IsAssignableFrom(underlying))
            {
                throw new InvalidOperationException(
                    $"BuildInMemorySource cannot project property '{prop.Name}' of type {prop.PropertyType}. " +
                    "Extend the projector to handle this type, or expose the policy via configuration section.");
            }

            var value = prop.GetValue(source);
            // Invariant culture: ConfigurationBinder parses with invariant; "0,3" would be rejected.
            data[prop.Name] = value switch
            {
                null => null,
                IFormattable fmt => fmt.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString(),
            };
        }

        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }
}
