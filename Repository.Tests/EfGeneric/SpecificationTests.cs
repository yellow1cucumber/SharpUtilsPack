using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using SharpUtils.Repository.Generic;
using SharpUtils.Results;
using System.Linq.Expressions;

namespace Repository.Tests.EfGeneric;

/// <summary>
/// Test specification implementation for unit testing.
/// </summary>
public class TestSpecification : ISpecification<TestEntity>
{
    public Expression<Func<TestEntity, bool>>? Criteria { get; set; }
    public IList<Expression<Func<TestEntity, object>>> Includes { get; set; } = new List<Expression<Func<TestEntity, object>>>();
    public Expression<Func<TestEntity, object>>? OrderBy { get; set; }
    public bool OrderByDescending { get; set; }
}

/// <summary>
/// Unit tests for specification pattern operations in EfGenericRepository.
/// Tests the GetBySpecification and GetBySpecificationAsync methods.
/// </summary>
public class SpecificationTests : IDisposable
{
    private readonly TestDbContext context;
    private readonly EfGenericRepository<TestEntity, int> repository;

    public SpecificationTests()
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
    public async Task GetBySpecification_WithCriteriaOnly_ShouldReturnMatchingEntities()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Active Entity 1", isActive: true),
            CreateTestEntity("Active Entity 2", isActive: true),
            CreateTestEntity("Inactive Entity", isActive: false)
        };
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        var specification = new TestSpecification
        {
            Criteria = e => e.IsActive
        };

        // Act
        var result = this.repository.GetBySpecification(specification);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count().Should().Be(2);
        result.Value!.Should().AllSatisfy(e => e.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task GetBySpecificationAsync_WithCriteriaOnly_ShouldReturnMatchingEntities()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Electronics Product 1", category: "Electronics"),
            CreateTestEntity("Electronics Product 2", category: "Electronics"),
            CreateTestEntity("Clothing Product", category: "Clothing")
        };
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        var specification = new TestSpecification
        {
            Criteria = e => e.Category == "Electronics"
        };

        // Act
        var result = await this.repository.GetBySpecificationAsync(specification);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count().Should().Be(2);
        result.Value!.Should().AllSatisfy(e => e.Category.Should().Be("Electronics"));
    }

    [Fact]
    public async Task GetBySpecification_WithNoCriteria_ShouldReturnAllEntities()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2"),
            CreateTestEntity("Entity 3")
        };
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        var specification = new TestSpecification(); // No criteria

        // Act
        var result = this.repository.GetBySpecification(specification);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count().Should().Be(3);
    }

    [Fact]
    public void GetBySpecification_WithNullSpecification_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.GetBySpecification(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to get entities by specification");
    }

    [Fact]
    public async Task GetBySpecificationAsync_WithNullSpecification_ShouldReturnFailureResult()
    {
        // Act
        var result = await this.repository.GetBySpecificationAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to get entities by specification");
    }

    [Fact]
    public async Task GetBySpecification_WithComplexCriteria_ShouldReturnMatchingEntities()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Expensive Active Product", isActive: true, price: 100.00m),
            CreateTestEntity("Cheap Active Product", isActive: true, price: 5.00m),
            CreateTestEntity("Expensive Inactive Product", isActive: false, price: 150.00m)
        };
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        var specification = new TestSpecification
        {
            Criteria = e => e.IsActive && e.Price > 50.00m
        };

        // Act
        var result = this.repository.GetBySpecification(specification);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count().Should().Be(1);
        result.Value!.First().Name.Should().Be("Expensive Active Product");
    }

    [Fact]
    public async Task GetBySpecification_WithEmptyResult_ShouldReturnEmptyCollection()
    {
        // Arrange
        var entity = CreateTestEntity("Test Entity", isActive: false);
        await this.context.TestEntities.AddAsync(entity);
        await this.context.SaveChangesAsync();

        var specification = new TestSpecification
        {
            Criteria = e => e.IsActive && e.Price > 1000.00m
        };

        // Act
        var result = this.repository.GetBySpecification(specification);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().BeEmpty();
    }

    private static TestEntity CreateTestEntity(string name, bool isActive = true, decimal price = 10.00m, string category = "Test")
    {
        return new TestEntity
        {
            Name = name,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            Category = category,
            Price = price
        };
    }
}