using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using SharpUtils.Repository.Generic;
using SharpUtils.Results;

namespace Repository.Tests.EfGeneric;

/// <summary>
/// Unit tests for streaming operations in EfGenericRepository.
/// Tests the GetAllAsStreamAsync method and related streaming functionality.
/// </summary>
public class StreamingTests : IDisposable
{
    private readonly TestDbContext context;
    private readonly EfGenericRepository<TestEntity, int> repository;

    public StreamingTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        this.context = new TestDbContext(options);
        this.repository = new EfGenericRepository<TestEntity, int>(this.context);
    }

    public void Dispose()
    {
        this.repository?.Dispose();
        this.context?.Dispose();
    }

    [Fact]
    public async Task GetAllAsStreamAsync_WithEntities_ShouldStreamAllEntities()
    {
        // Arrange
        var entities = Enumerable.Range(1, 10)
            .Select(i => CreateTestEntity($"Streaming Entity {i}"))
            .ToArray();
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        var results = new List<Result<TestEntity>>();

        // Act
        await foreach (var result in this.repository.GetAllAsStreamAsync(batchSize: 3))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(10);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        
        var streamedEntities = results.Select(r => r.Value!).ToList();
        streamedEntities.Should().HaveCount(10);
        streamedEntities.Select(e => e.Name).Should().Contain("Streaming Entity 1");
    }

    [Fact]
    public async Task GetAllAsStreamAsync_WithEmptyDatabase_ShouldReturnEmptyStream()
    {
        // Arrange
        var results = new List<Result<TestEntity>>();

        // Act
        await foreach (var result in this.repository.GetAllAsStreamAsync())
        {
            results.Add(result);
        }

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void GetAllAsStreamAsync_WithInvalidBatchSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            this.repository.GetAllAsStreamAsync(batchSize: 0));
    }

    [Fact]
    public async Task GetAllAsStreamAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var entities = Enumerable.Range(1, 100)
            .Select(i => CreateTestEntity($"Entity {i}"))
            .ToArray();
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        using var cts = new CancellationTokenSource();
        var results = new List<Result<TestEntity>>();
        var processedCount = 0;

        // Act
        try
        {
            await foreach (var result in this.repository.GetAllAsStreamAsync(batchSize: 10, cts.Token))
            {
                results.Add(result);
                processedCount++;
                
                if (processedCount >= 15)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }

        // Assert
        results.Should().HaveCount(15);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
    }

    private static TestEntity CreateTestEntity(string name)
    {
        return new TestEntity
        {
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Category = "Test",
            Price = 10.00m
        };
    }
}