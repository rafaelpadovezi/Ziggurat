namespace Newgrange
{
    public interface IMessage
    {
        string MessageId { get; set; }
        string MessageGroup { get; set; }
    }
}