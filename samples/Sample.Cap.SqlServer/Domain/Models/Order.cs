using System;

namespace Sample.Cap.SqlServer.Domain.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; }
}