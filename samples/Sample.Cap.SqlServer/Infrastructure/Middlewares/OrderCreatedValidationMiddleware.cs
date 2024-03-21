using Microsoft.Extensions.Logging;
using Sample.Cap.SqlServer.Dtos;
using System.Threading.Tasks;
using Ziggurat;

namespace Sample.Cap.SqlServer.Infrastructure.Middlewares;

public class OrderCreatedValidationMiddleware : IConsumerMiddleware<OrderCreatedMessage>
{
    private readonly ILogger<OrderCreatedValidationMiddleware> _logger;

    public OrderCreatedValidationMiddleware(ILogger<OrderCreatedValidationMiddleware> logger)
    {
        _logger = logger;
    }
    
    public Task OnExecutingAsync(OrderCreatedMessage message, ConsumerServiceDelegate<OrderCreatedMessage> next)
    {
        if (string.IsNullOrWhiteSpace(message.Number))
        {
            _logger.LogWarning("Invalid message: {MessageId}.", message.MessageId);
            return Task.CompletedTask;
        }

        return next(message);
    }
}