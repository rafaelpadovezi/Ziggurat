using DotNetCore.CAP;
using Ziggurat;

public class Consumer : ICapSubscribe
{
    private readonly IConsumerService<MyMessage> _service;

    public Consumer(IConsumerService<MyMessage> service)
    {
        _service = service;
    }

    [CapSubscribe("message", Group = "sample.rabbitmq.mongodb")]
    public async Task ConsumeMessage(MyMessage message)
    {
        await _service.ProcessMessageAsync(message);
    }
}

public class ConsumerService : IConsumerService<MyMessage>
{
    private readonly ILogger<ConsumerService> _logger;

    public ConsumerService(ILogger<ConsumerService> logger)
    {
        _logger = logger;
    }

    public Task ProcessMessageAsync(MyMessage message)
    {
        _logger.LogInformation(message.Text);
        return Task.CompletedTask;
    }
}