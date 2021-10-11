using Example.Cap.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Ziggurat.Idempotency;

namespace Example.Cap.Api.Infrastructure
{
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
}