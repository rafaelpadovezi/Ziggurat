using Microsoft.EntityFrameworkCore;
using Sample.Cap.SqlServer.Domain.Models;
using Ziggurat.SqlServer;

namespace Sample.Cap.SqlServer.Infrastructure;

public class ExampleDbContext : DbContext
{
    public ExampleDbContext(DbContextOptions<ExampleDbContext> options)
        : base(options)
    {
    }

    public DbSet<MessageTracking> Messages { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.MapMessageTracker();
    }
}