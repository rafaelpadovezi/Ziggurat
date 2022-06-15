namespace Ziggurat.SqlServer;

public class MessageTracking
{
    public MessageTracking(string id, string type)
    {
        Id = id;
        Type = type;
    }

    public string Id { get; private set; }
    public string Type { get; private set; }
    public DateTime DateTime { get; private set; } = DateTime.Now;
}