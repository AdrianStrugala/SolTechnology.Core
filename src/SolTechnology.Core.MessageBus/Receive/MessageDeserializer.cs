using System.Text;
using Newtonsoft.Json;

namespace SolTechnology.Core.MessageBus.Receive;

/// <summary>Reads a received message body as JSON of its registered message type.</summary>
internal static class MessageDeserializer
{
    public static DeserializedMessage Deserialize(BinaryData body, Type messageType)
    {
        var text = Encoding.UTF8.GetString(body);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new DeserializedMessage(null, DeserializationFailure.EmptyBody, null);
        }

        try
        {
            var payload = JsonConvert.DeserializeObject(text, messageType);
            return payload is null
                ? new DeserializedMessage(null, DeserializationFailure.NullPayload, null)
                : new DeserializedMessage(payload, DeserializationFailure.None, null);
        }
        catch (JsonException jsonException)
        {
            return new DeserializedMessage(null, DeserializationFailure.InvalidJson, jsonException);
        }
    }
}

internal enum DeserializationFailure
{
    None,
    EmptyBody,
    InvalidJson,
    NullPayload
}

internal sealed record DeserializedMessage(object? Payload, DeserializationFailure Failure, JsonException? Error);
