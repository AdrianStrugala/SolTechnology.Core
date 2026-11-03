using System.Text.Json;
using System.Text.Json.Serialization;

namespace SolTechnology.Core.AUID;

/// <summary>
/// JSON converter for Auid type.
/// Serializes Auid as a string using ToString() and deserializes using TryParse(), as a value or a dictionary key.
/// Invalid or empty input throws <see cref="JsonException"/>.
/// </summary>
public sealed class AuidJsonConverter : JsonConverter<Auid>
{
    /// <summary>Reads an AUID from a JSON string value.</summary>
    /// <exception cref="JsonException">Thrown when the token is not a string or the string is not a valid AUID.</exception>
    public override Auid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Unable to convert JSON value to Auid. Expected string, got {reader.TokenType}");
        }

        return ParseOrThrow(reader.GetString());
    }

    /// <summary>Writes the AUID as a JSON string value in the COD_YYYYMMDDHHmmss_RANDOM format.</summary>
    public override void Write(Utf8JsonWriter writer, Auid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    /// <summary>Reads an AUID from a JSON property name, e.g. a <c>Dictionary&lt;Auid, T&gt;</c> key.</summary>
    /// <exception cref="JsonException">Thrown when the property name is not a valid AUID.</exception>
    public override Auid ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ParseOrThrow(reader.GetString());
    }

    /// <summary>Writes the AUID as a JSON property name, e.g. a <c>Dictionary&lt;Auid, T&gt;</c> key.</summary>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, Auid value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }

    private static Auid ParseOrThrow(string? value)
    {
        if (Auid.TryParse(value, null, out var auid))
        {
            return auid;
        }

        throw new JsonException($"Unable to convert [{value}] to Auid. Expected format COD_YYYYMMDDHHmmss_RANDOM.");
    }
}
