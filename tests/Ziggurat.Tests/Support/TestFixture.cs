using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Ziggurat.Idempotency;

namespace Ziggurat.Tests.Support
{
    [Collection("TextFixture Collection")]
    public class TestFixture : IDisposable
    {
        private bool _isDisposed;

        public TestFixture()
        {
            Context.Database.EnsureCreated();
            Context.Database.BeginTransaction();
        }

        protected TestDbContext Context { get; } = new();

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                Context.Database.RollbackTransaction();
                Context?.Dispose();
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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