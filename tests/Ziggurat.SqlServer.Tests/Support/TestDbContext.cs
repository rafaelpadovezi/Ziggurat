using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Ziggurat.SqlServer.Tests.Support;

public class TestDbContext : DbContext
{
    public DbSet<MessageTracking> Messages { get; set; }
    public DbSet<MessageTracking> OtherEntity { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlServer("Server=localhost,5100;Initial Catalog=TestDb;User ID=sa;Password=Password1;;TrustServerCertificate=True")
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