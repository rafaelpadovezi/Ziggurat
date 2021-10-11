using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Ziggurat.Idempotency;

namespace Ziggurat.Tests.Support
{
    public class TestFixture : IDisposable
    {
        public TestFixture()
        {
            Context.Database.EnsureCreated();
            Context.Database.BeginTransaction();
        }

        protected TestDbContext Context { get; } = new();

        public void Dispose()
        {
            Context.Database.RollbackTransaction();
            Context?.Dispose();
        }
    }

    public class TestDbContext : DbContext
    {
        public DbSet<MessageTracking> Messages { get; set; }
        public DbSet<MessageTracking> OtherEntity { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlServer("Server=localhost,5100;Initial Catalog=TestDb;User ID=sa;Password=Password1;")
                .LogTo(message => Debug.WriteLine(message));
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

    public class OtherEntity
    {
        public int Id { get; set; }
        public string Code { get; set; }
    }
}