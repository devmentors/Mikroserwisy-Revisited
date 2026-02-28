using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TicketFlow.Shared.Serialization;

internal sealed class JsonSerializer : ISerializer
{
    public string Serialize(object obj)
        => System.Text.Json.JsonSerializer.Serialize(obj, SerializationOptions.Default);

    public TObject? Deserialize<TObject>(string json)
        => System.Text.Json.JsonSerializer.Deserialize<TObject>(json, SerializationOptions.Default);

    public object Deserialize(string json, Type obj)
       => System.Text.Json.JsonSerializer.Deserialize(json, obj, SerializationOptions.Default)!;

    public byte[] SerializeBinary(object @object)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(@object, SerializationOptions.Default);
        return Encoding.UTF8.GetBytes(json);
    }

    public TObject? DeserializeBinary<TObject>(byte[] objectBytes)
    {
        var json = Encoding.UTF8.GetString(objectBytes);
        return System.Text.Json.JsonSerializer.Deserialize<TObject>(json, SerializationOptions.Default);
    }
}