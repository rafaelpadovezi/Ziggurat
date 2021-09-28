using Microsoft.EntityFrameworkCore;

namespace Newgrange.Idempotency
{
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder MapMessageTracker(this ModelBuilder builder)
        {
            return builder.Entity<MessageTracking>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(255);
                entity.Property(e => e.Type).HasMaxLength(255);

                entity.HasKey(e => new
                {
                    e.Id,
                    e.Type
                });
            });
        }
    }
}