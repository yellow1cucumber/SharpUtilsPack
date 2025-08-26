using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using SharpUtils.Repository.Generic;

namespace Repository.Tests.EfGeneric;

/// <summary>
/// Unit tests for projection operations in EfGenericRepository.
/// Tests the GetProjectionAsync method and related projection functionality.
/// </summary>
public class ProjectionTests : IDisposable
{
    private readonly TestDbContext context;
    private readonly EfGenericRepository<TestEntity, int> repository;

    public ProjectionTests()
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

    /// <summary>
    /// Test DTO for projection tests.
    /// </summary>
    public class TestEntityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    [Fact]
    public async Task GetProjectionAsync_WithValidProjection_ShouldReturnProjectedResult()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Active Entity", isActive: true),
            CreateTestEntity("Inactive Entity", isActive: false)
        };
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        // Act
        var result = await this.repository.GetProjectionAsync(
            projection: e => new TestEntityDto { Id = e.Id, Name = e.Name, Category = e.Category }, 
            predicate: e => e.IsActive);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Active Entity");
        result.Value.Category.Should().Be("Test");
    }

    [Fact]
    public async Task GetProjectionAsync_WithNoMatchingEntities_ShouldReturnNull()
    {
        // Arrange
        var entity = CreateTestEntity("Inactive Entity", isActive: false);
        await this.context.TestEntities.AddAsync(entity);
        await this.context.SaveChangesAsync();

        // Act
        var result = await this.repository.GetProjectionAsync(
            projection: e => new TestEntityDto { Id = e.Id, Name = e.Name }, 
            predicate: e => e.IsActive);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No entity matches");
    }

    [Fact]
    public async Task GetProjectionAsync_WithScalarProjection_ShouldReturnScalarValue()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity 1", price: 10.50m),
            CreateTestEntity("Entity 2", price: 20.75m),
            CreateTestEntity("Entity 3", price: 5.25m)
        };
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        // Act
        var result = await this.repository.GetProjectionAsync(
            projection: e => e.Price, 
            predicate: e => e.Name == "Entity 2");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(20.75m);
    }

    [Fact]
    public async Task GetProjectionAsync_WithNullPredicate_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var result = await this.repository.GetProjectionAsync(
            projection: e => e.Name, predicate: null!);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("predicate").And.Contain("cannot be null");
    }

    [Fact]
    public async Task GetProjectionAsync_WithNullProjection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var result = await this.repository.GetProjectionAsync<string>(
            projection: null!, predicate: e => e.IsActive);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("projection").And.Contain("cannot be null");
    }

    private static TestEntity CreateTestEntity(string name, bool isActive = true, decimal price = 10.00m)
    {
        return new TestEntity
        {
            Name = name,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            Category = "Test",
            Price = price
        };
    }
}