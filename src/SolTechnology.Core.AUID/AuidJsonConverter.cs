using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// JSON converter for Auid type.
/// Serializes Auid as a string using ToString() and deserializes using TryParse(), as a value or a dictionary key.
/// Invalid or empty input throws <see cref="JsonException"/>.
/// </summary>
public class AuidJsonConverter : JsonConverter<Auid>
{
    public override Auid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Unable to convert JSON value to Auid. Expected string, got {reader.TokenType}");
        }

        return ParseOrThrow(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, Auid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    public override Auid ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ParseOrThrow(reader.GetString());
    }

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
