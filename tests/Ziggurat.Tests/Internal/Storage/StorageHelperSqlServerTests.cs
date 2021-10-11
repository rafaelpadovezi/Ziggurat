using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Ziggurat.Idempotency;
using Ziggurat.Internal.Storage;
using Ziggurat.Tests.Support;

namespace Ziggurat.Tests.Internal.Storage
{
    public class StorageHelperSqlServerTests : TestFixture
    {
        [Fact]
        public void IsMessageExistsError_UniqueConstraintMessageTracking_ShouldBeTrue()
        {
            // Arrange
            var helper = new StorageHelperSqlServer();
            Context.Add(new MessageTracking("1436814771495108608", "test.queue"));
            Context.SaveChanges();
            Context.DetachAllEntities(); // remove tracking so EF allows add entity with same Id
            Context.Add(new MessageTracking("1436814771495108608", "test.queue"));
            DbUpdateException ex = null;

            // Act & Assert
            try
            {
                Context.SaveChanges();
            }
            catch (DbUpdateException dbUpdateException)
            {
                ex = dbUpdateException;
            }

            ex.Should().NotBeNull();
            helper.IsMessageExistsError(ex).Should().BeTrue();
        }

        [Fact]
        public void IsMessageExistsError_MaxLenghtErrorMessageTracking_ShouldBeFalse()
        {
            // Arrange
            var helper = new StorageHelperSqlServer();
            Context.Add(new MessageTracking("1436814771495108608", new string('A', 1000)));
            DbUpdateException ex = null;

            // Act & Assert
            try
            {
                Context.SaveChanges();
            }
            catch (DbUpdateException dbUpdateException)
            {
                ex = dbUpdateException;
            }

            ex.Should().NotBeNull();
            helper.IsMessageExistsError(ex).Should().BeFalse();
        }

        [Fact]
        public void IsMessageExistsError_UniqueConstraintOtherEntity_ShouldBeFalse()
        {
            // Arrange
            var helper = new StorageHelperSqlServer();
            var tracking = new MessageTracking("1436814771495108608", "test.queue");
            Context.Add(tracking);
            // OtherEntity.Code should be unique
            Context.Add(new OtherEntity { Id = 1, Code = "1" });
            Context.Add(new OtherEntity { Id = 2, Code = "1" });
            DbUpdateException ex = null;

            // Act & Assert
            try
            {
                Context.SaveChanges();
            }
            catch (DbUpdateException dbUpdateException)
            {
                ex = dbUpdateException;
            }

            ex.Should().NotBeNull();
            helper.IsMessageExistsError(ex).Should().BeFalse();
        }
    }
}