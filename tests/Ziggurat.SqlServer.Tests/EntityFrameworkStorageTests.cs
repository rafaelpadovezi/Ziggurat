using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Ziggurat.SqlServer.Tests.Support;

namespace Ziggurat.SqlServer.Tests;

public class EntityFrameworkStorageTests : TestFixture
{
    private readonly EntityFrameworkStorage<TestDbContext> _storage;

    public EntityFrameworkStorageTests()
    {
        _storage = new(Context);
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

        Assert.NotNull(ex);
        Assert.True(_storage.IsMessageExistsError(ex));
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

        Assert.NotNull(ex);
        Assert.False(_storage.IsMessageExistsError(ex));
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

        Assert.NotNull(ex);
        Assert.False(_storage.IsMessageExistsError(ex));
    }

    [Fact]
    public void IsMessageExistsError_InvalidOperationException_ShouldBeFalse()
    {
        // Act & Assert
        Assert.False(_storage.IsMessageExistsError(new InvalidOperationException()));
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
        Assert.True(result);
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
        Assert.False(result);
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
        Assert.False(result);
    }

    [Fact]
    public async Task HasProcessedAsync_MessageIsNew_ShouldTrackMessage()
    {
        // Arrange & Act
        var result = await _storage.HasProcessedAsync(new TestMessage("1436814771495108608", "test.queue"));

        // Arrange
        Assert.False(result);
        var entity = Context.ChangeTracker.Entries().Single().Entity;
        Assert.Equivalent(new
        {
            Id = "1436814771495108608",
            Type = "test.queue"
        }, entity);
    }

    [Fact]
    public void Ctor_ContextWithoutMessageDbSet_ThrowException()
    {
        // Arrange
        var context = new TestContextWithoutMessages();

        // Act
        Action act = () => _ = new EntityFrameworkStorage<TestContextWithoutMessages>(context);

        // Assert
        const string expectedMessage =
            "Cannot create IdempotencyService because a DbSet for 'MessageTracking' is not included in the model for the context.";
        var exceptionMessage = Assert.Throws<InvalidOperationException>(act);
        Assert.Equal(expectedMessage, exceptionMessage.Message);
    }

    [Fact]
    public async Task DeleteMessagesHistoryOlderThanAsync_OldThan30Days_ShouldReturn3()
    {
        // Arrange
        var tracking1 = new MessageTracking("1436814771495108601", "test.queue");
        var tracking2 = new MessageTracking("1436814771495108602", "test.queue");
        var tracking3 = new MessageTracking("1436814771495108603", "test.queue");
        var tracking4 = new MessageTracking("1436814771495108604", "test.queue");
        var tracking5 = new MessageTracking("1436814771495108605", "test.queue");
        var tracking6 = new MessageTracking("1436814771495108606", "test.queue");
        Context.AddRange(tracking1, tracking2, tracking3, tracking4, tracking5, tracking6);
        await Context.SaveChangesAsync();

        // update timestamp of messages to be deleted
        await Context.Database.ExecuteSqlInterpolatedAsync($"UPDATE MessageTracking SET DateTime = {DateTime.Now.AddDays(-60)} WHERE Id IN ('1436814771495108604','1436814771495108605','1436814771495108606')");

        // Act
        var result = await _storage.DeleteMessagesHistoryOlderThanAsync(30, 100, default);

        // Assert
        Assert.Equal(3, result);
        Assert.Equal(3, Context.Messages.Count());
        var dbMessages = await Context.Messages.ToListAsync();
        Assert.Collection(dbMessages,
            x => Assert.Equal("1436814771495108601", x.Id),
            x => Assert.Equal("1436814771495108602", x.Id),
            x => Assert.Equal("1436814771495108603", x.Id));

    }

    [Fact]
    public async Task DeleteMessagesHistoryOlderThanAsync_WithBatchSize3_ShouldReturn3()
    {
        // Arrange
        var tracking1 = new MessageTracking("1436814771495108601", "test.queue");
        var tracking2 = new MessageTracking("1436814771495108602", "test.queue");
        var tracking3 = new MessageTracking("1436814771495108603", "test.queue");
        var tracking4 = new MessageTracking("1436814771495108604", "test.queue");
        var tracking5 = new MessageTracking("1436814771495108605", "test.queue");
        var tracking6 = new MessageTracking("1436814771495108606", "test.queue");
        Context.AddRange(tracking1, tracking2, tracking3, tracking4, tracking5, tracking6);
        await Context.SaveChangesAsync();

        // update timestamp of messages to be deleted
        await Context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE MessageTracking SET DateTime = {DateTime.Now.AddDays(-60)} WHERE Id IN ('1436814771495108601','1436814771495108602','1436814771495108603', '1436814771495108604','1436814771495108605','1436814771495108606')");

        // Act
        var result = await _storage.DeleteMessagesHistoryOlderThanAsync(30, 3, default);

        // Assert
        // Assert
        Assert.Equal(3, result);
        Assert.Equal(3, Context.Messages.Count());
    }

    [Fact]
    public async Task DeleteMessagesHistoryOlderThanAsync_WhenCalledAtSameTime_ShouldDeleteDifferentRecords()
    {
        // Arrange
        // create a new DB context to avoid the test transaction
        await using var dbContext = new TestDbContext();
        var tracking1 = new MessageTracking("1436814771495108601", "test.queue");
        var tracking2 = new MessageTracking("1436814771495108602", "test.queue");
        var tracking3 = new MessageTracking("1436814771495108603", "test.queue");
        var tracking4 = new MessageTracking("1436814771495108604", "test.queue");
        var tracking5 = new MessageTracking("1436814771495108605", "test.queue");
        var tracking6 = new MessageTracking("1436814771495108606", "test.queue");
        dbContext.AddRange(tracking1, tracking2, tracking3, tracking4, tracking5, tracking6);
        await dbContext.SaveChangesAsync();

        // update timestamp of messages to be deleted
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE MessageTracking SET DateTime = {DateTime.Now.AddDays(-60)} WHERE Id IN ('1436814771495108601','1436814771495108602','1436814771495108603', '1436814771495108604','1436814771495108605','1436814771495108606')");


        // create two storages with different contexts to allow run async queries in parallel
        await using var dbContext1 = new TestDbContext();
        await using var dbContext2 = new TestDbContext();
        var storage1 = new EntityFrameworkStorage<TestDbContext>(dbContext1);
        var storage2 = new EntityFrameworkStorage<TestDbContext>(dbContext2);

        // Act

        var results = await Task.WhenAll(
            storage1.DeleteMessagesHistoryOlderThanAsync(30, 3, CancellationToken.None),
            storage2.DeleteMessagesHistoryOlderThanAsync(30, 3, CancellationToken.None)
        );

        // Assert
        Assert.Equal(new[] { 3, 3 }, results);
        Assert.Equal(0, await dbContext.Messages.AsNoTracking().CountAsync());
    }
}