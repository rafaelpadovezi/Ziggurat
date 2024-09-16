using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace Ziggurat.SqlServer.Tests.Support;

public class TestDbContext : DbContext
{
    public DbSet<MessageTracking> Messages { get; set; }
    public DbSet<MessageTracking> OtherEntity { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = "Server=localhost;Database=TestDb;User=sa;Password=Password1;TrustServerCertificate=True";
        optionsBuilder
            .UseSqlServer(connectionString)
            .LogTo(message => Debug.WriteLine(message), LogLevel.Information)
            .EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.MapMessageTracker();

        modelBuilder.Entity<OtherEntity>(entity => { entity.HasIndex(x => x.Code).IsUnique(); });
    }

    public void DetachAllEntities()
    {
        foreach (var entry in ChangeTracker.Entries())
            entry.State = EntityState.Detached;
    }
}