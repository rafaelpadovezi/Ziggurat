using System;

namespace Ziggurat.MongoDB;

public class MessageTracking
{
    public MessageTracking(string id, string type)
    {
        Id = CreateId(id, type);
        MessageId = id;
        Type = type;
    }

    public string Id { get; set; }
    public string MessageId { get; set; }
    public string Type { get; set; }
    public DateTime DateTime { get; } = DateTime.Now;

    public static string CreateId(string id, string type)
    {
        return $"{id}_{type}";
    }
}