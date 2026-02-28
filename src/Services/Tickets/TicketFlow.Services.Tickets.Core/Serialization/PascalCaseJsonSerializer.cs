using System.Text;
using System.Text.Json;
using TicketFlow.Shared.Serialization;

namespace TicketFlow.Services.Tickets.Core.Serialization;

public sealed class PascalCaseJsonSerializer : ISerializer
{
    private static readonly JsonSerializerOptions PascalCaseOptions = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = false
    };

    public string Serialize(object obj)
        => JsonSerializer.Serialize(obj, PascalCaseOptions);

    public TObject? Deserialize<TObject>(string json)
        => JsonSerializer.Deserialize<TObject>(json, PascalCaseOptions);

    public object Deserialize(string json, Type obj)
        => JsonSerializer.Deserialize(json, obj, PascalCaseOptions)!;

    public byte[] SerializeBinary(object @object)
    {
        var json = JsonSerializer.Serialize(@object, PascalCaseOptions);
        return Encoding.UTF8.GetBytes(json);
    }

    public TObject? DeserializeBinary<TObject>(byte[] objectBytes)
    {
        var json = Encoding.UTF8.GetString(objectBytes);
        return JsonSerializer.Deserialize<TObject>(json, PascalCaseOptions);
    }
}
