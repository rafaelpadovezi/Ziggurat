using System;

namespace Example.Cap.Api.Domain.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; }
}