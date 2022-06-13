using DotNetCore.CAP;
using Ziggurat;

namespace Sample.Cap.Mongo;

public class Consumer : ICapSubscribe
{
    private readonly IConsumerService<MyMessage> _service;

    public Consumer(IConsumerService<MyMessage> service)
    {
        _service = service;
    }

    [CapSubscribe("myapp.paymentCondition.created", Group = "mongo.paymentCondition.created")]
    public async Task ConsumeMessage(MyMessage message)
    {
        await _service.ProcessMessageAsync(message);
    }
}