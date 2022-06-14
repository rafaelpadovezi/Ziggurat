namespace Ziggurat.SqlServer;

public class MessageTracking
{
    public MessageTracking(string id, string type)
    {
        Id = id;
        Type = type;
    }

    public string Id { get; set; }
    public string Type { get; set; }
    public DateTime DateTime { get; } = DateTime.Now;
}