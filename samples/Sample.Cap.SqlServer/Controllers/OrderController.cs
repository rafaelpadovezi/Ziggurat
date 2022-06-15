using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using Sample.Cap.SqlServer.Domain.Models;
using Sample.Cap.SqlServer.Infrastructure;
using System.Threading.Tasks;

namespace Sample.Cap.SqlServer.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly ICapPublisher _capBus;
    private readonly ExampleDbContext _context;

    public OrderController(
        ICapPublisher capPublisher,
        ExampleDbContext context)
    {
        _context = context;
        _capBus = capPublisher;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] Order order)
    {
        _context.Orders.Add(order);

        await using (_context.Database.BeginTransaction(_capBus, true))
        {
            await _capBus.PublishAsync("order.created", new { order.Number });

            await _context.SaveChangesAsync();
        }

        return Ok();
    }
}