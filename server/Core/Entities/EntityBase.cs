using System.Text.Json;

namespace RiotProxy.Core.Entities;

public abstract class EntityBase
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true
    };

    public string ToJson()
    {
        // Use runtime type so derived properties are included
        return JsonSerializer.Serialize(this, GetType(), s_options);
    }

    public override string ToString() => ToJson();
}

