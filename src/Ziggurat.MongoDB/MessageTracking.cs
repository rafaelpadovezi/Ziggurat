namespace Ziggurat.MongoDB;

public class MessageTracking
{
    public MessageTracking()
    { }

    public MessageTracking(string id, string type)
    {
        Id = CreateId(id, type);
        MessageId = id;
        Type = type;
    }

    public string Id { get; private set; }
    public string MessageId { get; private set; }
    public string Type { get; private set; }
    public DateTime DateTime { get; private set; } = DateTime.Now;

    public static string CreateId(string id, string type)
    {
        return $"{id}_{type}";
    }
}