using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    public void IsMessageExistsError_InvalidOperationException_ShouldBeFalse()
    {
        // Act & Assert
        _storage.IsMessageExistsError(new InvalidOperationException()).Should().BeFalse();
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

    [Fact]
    public async Task IsDeleteHistoryMessages_OltherThan30Days_ShouldUpdateDatabase()
    {
        // Arrange
        var dbRealContext = new TestDbContext();
        var storageWithRealDbContext = new EntityFrameworkStorage<TestDbContext>(dbRealContext);

        var tracking1 = new MessageTracking("1436814771495108601", "test.queue");
        var tracking2 = new MessageTracking("1436814771495108602", "test.queue");
        var tracking3 = new MessageTracking("1436814771495108603", "test.queue");
        var tracking4 = new MessageTracking("1436814771495108604", "test.queue");
        var tracking5 = new MessageTracking("1436814771495108605", "test.queue");
        var tracking6 = new MessageTracking("1436814771495108606", "test.queue");
        var listTrackings = new List<MessageTracking> { tracking1, tracking2, tracking3, tracking4, tracking5, tracking6 };
        dbRealContext.AddRange(listTrackings);
        await dbRealContext.SaveChangesAsync();

        await dbRealContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE MessageTracking SET DateTime = {DateTime.Now.AddDays(-60)} WHERE Id IN ('1436814771495108604','1436814771495108605','1436814771495108606')");

        // Act
        var count = await storageWithRealDbContext.DeleteMessagesHistoryOlderThanAsync(30, 0);
        await dbRealContext.SaveChangesAsync();

        // Assert
        dbRealContext.Messages.Count().Should().Be(3);
        count.Should().Be(3);
        dbRealContext.Messages.Where(m => m.Id == "1436814771495108601").Should().NotBeNull();
        dbRealContext.Messages.Where(m => m.Id == "1436814771495108602").Should().NotBeNull();
        dbRealContext.Messages.Where(m => m.Id == "1436814771495108603").Should().NotBeNull();
        dbRealContext.Messages.Where(m => m.Id == "1436814771495108601").Should().NotBeNull();
        dbRealContext.Messages.Where(m => m.Id == "1436814771495108604").Should().BeEmpty();
        dbRealContext.Messages.Where(m => m.Id == "1436814771495108605").Should().BeEmpty();
        dbRealContext.Messages.Where(m => m.Id == "1436814771495108606").Should().BeEmpty();

        await dbRealContext.Database.ExecuteSqlAsync($"TRUNCATE TABLE MessageTracking");
    }

    [Fact]
    public async Task IsDeleteHistoryMessages_OltherThan30DaysMax2_ShouldUpdateDatabase()
    {
        // Arrange
        var dbRealContext = new TestDbContext();
        var storageWithRealDbContext = new EntityFrameworkStorage<TestDbContext>(dbRealContext);

        var tracking1 = new MessageTracking("1436814771495108611", "test.queue");
        var tracking2 = new MessageTracking("1436814771495108612", "test.queue");
        var tracking3 = new MessageTracking("1436814771495108613", "test.queue");
        var tracking4 = new MessageTracking("1436814771495108614", "test.queue");
        var tracking5 = new MessageTracking("1436814771495108615", "test.queue");
        var tracking6 = new MessageTracking("1436814771495108616", "test.queue");
        var listTrackings = new List<MessageTracking> { tracking1, tracking2, tracking3, tracking4, tracking5, tracking6 };
        dbRealContext.AddRange(listTrackings);
        await dbRealContext.SaveChangesAsync();

        await dbRealContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE MessageTracking SET DateTime = {DateTime.Now.AddDays(-60)} WHERE Id IN ('1436814771495108614','1436814771495108615','14368147714951086016')");

        // Act
        var count = await storageWithRealDbContext.DeleteMessagesHistoryOlderThanAsync(30, 2);
        await dbRealContext.SaveChangesAsync();

        // Assert
        dbRealContext.Messages.Count().Should().Be(4);
        count.Should().Be(2);

        await dbRealContext.Database.ExecuteSqlAsync($"TRUNCATE TABLE MessageTracking");
    }
}