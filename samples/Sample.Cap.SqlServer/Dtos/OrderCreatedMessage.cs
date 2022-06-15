using Ziggurat;

namespace Sample.Cap.SqlServer.Dtos;

public record OrderCreatedMessage(string Number) : IMessage
{
    public string MessageId { get; set; }
    public string MessageGroup { get; set; }
}