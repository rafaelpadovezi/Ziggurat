using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Ziggurat.SqlServer.Tests.Support;

namespace Ziggurat.SqlServer.Tests;

public class EntityFrameworkStorageTests : TestFixture
{
    private readonly EntityFrameworkStorage<TestDbContext> _storage;

    public EntityFrameworkStorageTests()
    {
        _storage = new EntityFrameworkStorage<TestDbContext>(Context);
    }

    [Fact]
    public void IsMessageExistsError_UniqueConstraintMessageTracking_ShouldBeTrue()
    {
        // Arrange
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
        _storage.IsMessageExistsError(ex).Should().BeTrue();
    }

    [Fact]
    public void IsMessageExistsError_MaxLenghtErrorMessageTracking_ShouldBeFalse()
    {
        // Arrange
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
        _storage.IsMessageExistsError(ex).Should().BeFalse();
    }

    [Fact]
    public void IsMessageExistsError_UniqueConstraintOtherEntity_ShouldBeFalse()
    {
        // Arrange
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
        _storage.IsMessageExistsError(ex).Should().BeFalse();
    }

    [Fact]
    public async Task HasProcessedAsync_MessageIsRepeated_ReturnTrue()
    {
        // Arrange
        var tracking = new MessageTracking("1436814771495108608", "test.queue");
        Context.Add(tracking);
        await Context.SaveChangesAsync();
        Context.DetachAllEntities();

        // Act
        var result = await _storage.HasProcessedAsync(new TestMessage(tracking.Id, tracking.Type));

        // Arrange
        result.Should().Be(true);
    }

    [Fact]
    public async Task HasProcessedAsync_MessageHasDiffentQueue_ReturnFalse()
    {
        // Arrange
        var tracking = new MessageTracking("1436814771495108608", "test.queue");
        Context.Add(tracking);
        await Context.SaveChangesAsync();
        Context.DetachAllEntities();

        // Act
        var result = await _storage.HasProcessedAsync(new TestMessage(tracking.Id, "other-queue"));

        // Arrange
        result.Should().Be(false);
    }

    [Fact]
    public async Task HasProcessedAsync_MessageHasDifferentId_ReturnFalse()
    {
        // Arrange
        var tracking = new MessageTracking("1436814771495108608", "test.queue");
        Context.Add(tracking);
        await Context.SaveChangesAsync();
        Context.DetachAllEntities();

        // Act
        var result = await _storage.HasProcessedAsync(new TestMessage("new-message-id", tracking.Type));

        // Arrange
        result.Should().Be(false);
    }

    [Fact]
    public async Task HasProcessedAsync_MessageIsNew_ShouldTrackMessage()
    {
        // Arrange & Act
        var result = await _storage.HasProcessedAsync(new TestMessage("1436814771495108608", "test.queue"));

        // Arrange
        result.Should().Be(false);
        Context.ChangeTracker.Entries().Single().Entity.Should()
            .BeEquivalentTo(new
            {
                Id = "1436814771495108608",
                Type = "test.queue"
            });
    }

    [Fact]
    public void Ctor_ContextWithoutMessageDbSet_ThrowException()
    {
        // Arrange
        var context = new TestContextWithoutMessages();

        // Act
        Action act = () => _ = new EntityFrameworkStorage<TestContextWithoutMessages>(context);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(
            "Cannot create IdempotencyService because a DbSet for 'MessageTracking' is not included in the model for the context.");
    }
}