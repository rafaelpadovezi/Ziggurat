using DotNetCore.CAP;
using Sample.Cap.SqlServer.Dtos;
using System.Threading.Tasks;
using Ziggurat;

namespace Sample.Cap.SqlServer.Consumers;

public class OrderCreatedConsumer : ICapSubscribe
{
    private readonly IConsumerService<OrderCreatedMessage> _service;

    public OrderCreatedConsumer(IConsumerService<OrderCreatedMessage> service)
    {
        _service = service;
    }

    [CapSubscribe("order.created", Group = "catalog.order.created")]
    public async Task UpdateProductStock(OrderCreatedMessage message)
    {
        await _service.ProcessMessageAsync(message);
    }
}