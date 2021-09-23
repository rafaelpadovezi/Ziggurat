namespace DotNetCore.CAP.Contrib.Idempotency
{
    public interface IMessage
    {
        string MessageId { get; set; }
        string MessageGroup { get; set; }
    }
}