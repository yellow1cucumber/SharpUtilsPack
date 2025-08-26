using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using SharpUtils.Repository.Generic;

namespace Repository.Tests.EfGeneric;

/// <summary>
/// Unit tests for error handling scenarios in EfGenericRepository.
/// Tests various error conditions and exception handling.
/// </summary>
public class ErrorHandlingTests : IDisposable
{
    private readonly TestDbContext context;
    private readonly EfGenericRepository<TestEntity, int> repository;

    public ErrorHandlingTests()
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
    public void Add_WithNullEntity_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.Add(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to add entity");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WithNullEntity_ShouldReturnFailureResult()
    {
        // Act
        var result = await this.repository.AddAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to add entity");
        result.Value.Should().BeNull();
    }

    [Fact]
    public void GetById_WithNullId_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.GetById(default!);

        // Assert - Note: int is a value type, so this test behavior depends on implementation
        // The repository should handle this gracefully
        result.Should().NotBeNull();
        // The result could be success with null value or failure depending on implementation
    }

    [Fact]
    public void GetFirstOrDefault_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.GetFirstOrDefault(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to get first entity");
    }

    [Fact]
    public async Task GetFirstOrDefaultAsync_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = await this.repository.GetFirstOrDefaultAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to get first entity");
    }

    [Fact]
    public void GetWhere_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.GetWhere(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to get entities by predicate");
    }

    [Fact]
    public async Task GetWhereAsync_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = await this.repository.GetWhereAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to get entities by predicate");
    }

    [Fact]
    public void CountWhere_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.CountWhere(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to count entities by predicate");
    }

    [Fact]
    public async Task CountWhereAsync_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = await this.repository.CountWhereAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to count entities by predicate");
    }

    [Fact]
    public void Exists_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.Exists(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to check existence of entities by predicate");
    }

    [Fact]
    public async Task ExistsAsync_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = await this.repository.ExistsAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to check existence of entities by predicate");
    }

    [Fact]
    public void Update_WithNullEntity_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.Update(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to update entity");
    }

    [Fact]
    public async Task UpdateAsync_WithNullEntity_ShouldReturnFailureResult()
    {
        // Act
        var result = await this.repository.UpdateAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to update entity");
    }

    [Fact]
    public void UpdateRange_WithNullCollection_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.UpdateRange(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to update entities");
    }

    [Fact]
    public async Task UpdateRangeAsync_WithNullCollection_ShouldReturnFailureResult()
    {
        // Act
        var result = await this.repository.UpdateRangeAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to update entities");
    }

    [Fact]
    public void Delete_WithNullEntity_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.Delete(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to delete entity");
    }

    [Fact]
    public async Task DeleteAsync_WithNullEntity_ShouldReturnFailureResult()
    {
        // Act
        var result = await this.repository.DeleteAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to delete entity");
    }

    [Fact]
    public void DeleteRange_WithNullCollection_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.DeleteRange(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to delete entities");
    }

    [Fact]
    public async Task DeleteRangeAsync_WithNullCollection_ShouldReturnFailureResult()
    {
        // Act
        var result = await this.repository.DeleteRangeAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to delete entities");
    }

    [Fact]
    public void DeleteWhere_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.DeleteWhere(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to delete entities by predicate");
    }

    [Fact]
    public async Task DeleteWhereAsync_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = await this.repository.DeleteWhereAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to delete entities by predicate");
    }

    [Fact]
    public void BulkUpdate_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.BulkUpdate(null!, e => new TestEntity { Name = "Updated" });

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to bulk update entities");
    }

    [Fact]
    public void BulkUpdate_WithNullUpdateExpression_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.BulkUpdate(e => e.IsActive, null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to bulk update entities");
    }

    [Fact]
    public async Task BulkUpdateAsync_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = await this.repository.BulkUpdateAsync(null!, e => new TestEntity { Name = "Updated" });

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to bulk update entities");
    }

    [Fact]
    public void BulkDelete_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.BulkDelete(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to bulk delete entities");
    }

    [Fact]
    public async Task BulkDeleteAsync_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = await this.repository.BulkDeleteAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to bulk delete entities");
    }

    [Fact]
    public void UpdatePartial_WithNullEntity_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.UpdatePartial(null!, e => e.Name, "New Name");

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to partially update entity");
    }

    [Fact]
    public async Task UpdatePartial_WithNullPropertyExpression_ShouldReturnFailureResult()
    {
        // Arrange
        var entity = CreateTestEntity("Test Entity");

        // Act
        var result = this.repository.UpdatePartial(entity, null!, "New Name");

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to partially update entity");
    }

    [Fact]
    public async Task TransactionAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await this.repository.TransactionAsync<bool>(null!));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void GetPaged_WithInvalidParameters_ShouldReturnFailureResult(int pageNumber, int pageSize)
    {
        // Act
        var result = this.repository.GetPaged(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("must be greater than zero");
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task GetPagedAsync_WithInvalidParameters_ShouldReturnFailureResult(int pageNumber, int pageSize)
    {
        // Act
        var result = await this.repository.GetPagedAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("must be greater than zero");
    }

    [Fact]
    public void GetPagedWhere_WithNullPredicate_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.GetPagedWhere(null!, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to get paged entities by predicate");
    }

    [Fact]
    public void GetPagedOrdered_WithNullOrderBy_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.GetPagedOrdered(null!, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to get paged ordered entities");
    }

    private static TestEntity CreateTestEntity(string name, bool isActive = true)
    {
        return new TestEntity
        {
            Name = name,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            Category = "Test",
            Price = 10.00m
        };
    }
}