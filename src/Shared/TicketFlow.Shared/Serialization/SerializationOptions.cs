using System.Text.Json;
using System.Text.Json.Serialization;

namespace TicketFlow.Shared.Serialization;

public static class SerializationOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerOptions.Default)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}