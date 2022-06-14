using Ziggurat;

namespace Example.Cap.Api.Dtos;

public record OrderCreatedMessage(string Number) : IMessage
{
    public string MessageId { get; set; }
    public string MessageGroup { get; set; }
}