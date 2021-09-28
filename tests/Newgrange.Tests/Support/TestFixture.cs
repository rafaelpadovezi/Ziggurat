using Microsoft.EntityFrameworkCore;
using Newgrange.Idempotency;
using System;
using System.Diagnostics;

namespace Newgrange.Tests.Support
{
    public class TestFixture : IDisposable
    {
        protected TestDbContext Context { get; } = new();

        public TestFixture()
        {
            Context.Database.EnsureCreated();
            Context.Database.BeginTransaction();
        }

        public void Dispose()
        {
            Context.Database.RollbackTransaction();
            Context?.Dispose();
        }
    }

    public class TestDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlServer("Server=localhost,5100;Initial Catalog=TestDb;User ID=sa;Password=Password1;")
                .LogTo(message => Debug.WriteLine(message));
        }

        public DbSet<MessageTracking> Messages { get; set; }
        public DbSet<MessageTracking> OtherEntity { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.MapMessageTracker();

            modelBuilder.Entity<OtherEntity>(entity =>
            {
                entity.HasIndex(x => x.Code).IsUnique();
            });
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