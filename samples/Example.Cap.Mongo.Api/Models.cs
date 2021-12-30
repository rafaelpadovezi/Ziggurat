using Ziggurat;

public class MyMessage : IMessage
{
    public string Text { get; set; }
    public string MessageId { get; set; }
    public string MessageGroup { get; set; }
}