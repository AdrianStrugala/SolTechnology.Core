using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// JSON converter for Auid type.
/// Serializes Auid as a string using ToString() and deserializes using Parse().
/// </summary>
public class AuidJsonConverter : JsonConverter<Auid>
{
    public override Auid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrEmpty(stringValue))
            {
                return Auid.Empty;
            }

            return Auid.Parse(stringValue);
        }

        throw new JsonException($"Unable to convert JSON value to Auid. Expected string, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, Auid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
