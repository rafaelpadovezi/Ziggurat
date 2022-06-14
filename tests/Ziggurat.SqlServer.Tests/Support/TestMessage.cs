namespace Ziggurat.SqlServer.Tests.Support;

public class TestMessage : IMessage
{
    public TestMessage(string messageId, string messageGroup)
    {
        MessageId = messageId;
        MessageGroup = messageGroup;
    }

    public string MessageId { get; set; }
    public string MessageGroup { get; set; }
}