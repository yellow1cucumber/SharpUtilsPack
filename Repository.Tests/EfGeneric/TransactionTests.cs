using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using SharpUtils.Repository.Generic;
using SharpUtils.Results;
using Microsoft.Data.Sqlite;

namespace Repository.Tests.EfGeneric;

/// <summary>
/// Transaction and rollback tests for EfGenericRepository using SQLite database.
/// These tests require a real database provider that supports transactions, unlike InMemory database.
/// SQLite provides proper transaction support for testing real-world scenarios.
/// </summary>
public class TransactionTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly TestDbContext context;
    private readonly EfGenericRepository<TestEntity, int> repository;

    public TransactionTests()
    {
        // Create SQLite connection in memory mode
        this.connection = new SqliteConnection("DataSource=:memory:");
        this.connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(this.connection)
            .Options;

        this.context = new TestDbContext(options);
        this.context.Database.EnsureCreated();
        this.repository = new EfGenericRepository<TestEntity, int>(this.context);
    }

    public void Dispose()
    {
        this.repository?.Dispose();
        this.context?.Dispose();
        this.connection?.Dispose();
    }

    #region Transaction Success Tests

    [Fact]
    public async Task TransactionAsync_WithSuccessfulOperation_ShouldCommitChanges()
    {
        // Act
        var result = await this.repository.TransactionAsync(async repo =>
        {
            var entity1 = CreateTestEntity("Transaction Entity 1");
            var entity2 = CreateTestEntity("Transaction Entity 2");

            var addResult1 = await repo.AddAsync(entity1);
            var addResult2 = await repo.AddAsync(entity2);

            if (addResult1.IsFailure || addResult2.IsFailure)
                return Result<bool>.Failure("Failed to add entities");

            return Result<bool>.Success(true);
        });

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        // Verify entities were committed to database
        var allEntities = await this.repository.GetAllAsync();
        allEntities.IsSuccess.Should().BeTrue();
        allEntities.Value!.Should().HaveCount(2);
        allEntities.Value!.Should().Contain(e => e.Name == "Transaction Entity 1");
        allEntities.Value!.Should().Contain(e => e.Name == "Transaction Entity 2");
    }

    [Fact]
    public async Task TransactionAsync_WithMultipleOperations_ShouldCommitAllChanges()
    {
        // Arrange
        var existingEntity = CreateTestEntity("Existing Entity");
        await this.repository.AddAsync(existingEntity);
        await this.repository.SaveChangesAsync();

        // Act
        var result = await this.repository.TransactionAsync(async repo =>
        {
            // Add new entity
            var newEntity = CreateTestEntity("New Entity");
            var addResult = await repo.AddAsync(newEntity);

            // Update existing entity
            existingEntity.Name = "Updated Existing Entity";
            var updateResult = await repo.UpdateAsync(existingEntity);

            // Add another entity
            var anotherEntity = CreateTestEntity("Another Entity");
            var addResult2 = await repo.AddAsync(anotherEntity);

            if (addResult.IsFailure || updateResult.IsFailure || addResult2.IsFailure)
                return Result<int>.Failure("Failed to perform operations");

            return Result<int>.Success(3); // Total entities expected
        });

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);

        // Verify all changes were committed
        var allEntities = await this.repository.GetAllAsync();
        allEntities.IsSuccess.Should().BeTrue();
        allEntities.Value!.Should().HaveCount(3);
        allEntities.Value!.Should().Contain(e => e.Name == "Updated Existing Entity");
        allEntities.Value!.Should().Contain(e => e.Name == "New Entity");
        allEntities.Value!.Should().Contain(e => e.Name == "Another Entity");
    }

    [Fact]
    public async Task TransactionAsync_WithComplexBusinessLogic_ShouldMaintainConsistency()
    {
        // Act - Simulate a business operation with multiple database changes
        var result = await this.repository.TransactionAsync(async repo =>
        {
            // Create a batch of related entities
            var entities = new List<TestEntity>
            {
                CreateTestEntity("Product 1", price: 100.00m, category: "Electronics"),
                CreateTestEntity("Product 2", price: 200.00m, category: "Electronics"),
                CreateTestEntity("Product 3", price: 50.00m, category: "Books")
            };

            // Add all entities
            var addResult = await repo.AddRangeAsync(entities);
            if (addResult.IsFailure)
                return Result<decimal>.Failure("Failed to add products");

            // Calculate total value
            var totalValue = entities.Sum(e => e.Price);

            // Simulate business rule: if total value > 300, apply discount
            if (totalValue > 300m)
            {
                foreach (var entity in entities.Where(e => e.Category == "Electronics"))
                {
                    entity.Price *= 0.9m; // 10% discount
                    await repo.UpdateAsync(entity);
                }
            }

            return Result<decimal>.Success(totalValue);
        });

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(350.00m);

        // Verify business logic was applied correctly
        var electronicsProducts = await this.repository.GetWhereAsync(e => e.Category == "Electronics");
        electronicsProducts.IsSuccess.Should().BeTrue();
        electronicsProducts.Value!.Should().AllSatisfy(e => e.Price.Should().BeApproximately(e.Price, 0.01m));

        var bookProducts = await this.repository.GetWhereAsync(e => e.Category == "Books");
        bookProducts.IsSuccess.Should().BeTrue();
        bookProducts.Value!.First().Price.Should().Be(50.00m); // No discount applied
    }

    #endregion

    #region Transaction Failure Tests

    [Fact]
    public async Task TransactionAsync_WithFailedOperation_ShouldRollbackChanges()
    {
        // Arrange
        var initialEntity = CreateTestEntity("Initial Entity");
        await this.repository.AddAsync(initialEntity);
        await this.repository.SaveChangesAsync();

        // Act
        var result = await this.repository.TransactionAsync(async repo =>
        {
            var entity1 = CreateTestEntity("Transaction Entity 1");
            var entity2 = CreateTestEntity("Transaction Entity 2");

            var addResult1 = await repo.AddAsync(entity1);
            var addResult2 = await repo.AddAsync(entity2);

            if (addResult1.IsSuccess && addResult2.IsSuccess)
            {
                // Simulate a business rule failure
                return Result<bool>.Failure("Business rule validation failed");
            }

            return Result<bool>.Success(true);
        });

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Business rule validation failed");

        // Verify transaction was rolled back - only initial entity should remain
        var allEntities = await this.repository.GetAllAsync();
        allEntities.IsSuccess.Should().BeTrue();
        allEntities.Value!.Should().HaveCount(1);
        allEntities.Value!.First().Name.Should().Be("Initial Entity");
    }

    [Fact]
    public async Task TransactionAsync_WithException_ShouldRollbackChanges()
    {
        // Arrange
        var initialCount = await this.repository.CountAsync();

        // Act
        var result = await this.repository.TransactionAsync(async repo =>
        {
            var entity1 = CreateTestEntity("Entity 1");
            var entity2 = CreateTestEntity("Entity 2");

            await repo.AddAsync(entity1);
            await repo.AddAsync(entity2);

            // Simulate an exception during the operation
            throw new InvalidOperationException("Simulated exception");

#pragma warning disable CS0162 // Unreachable code detected
            return Result<bool>.Success(true);
#pragma warning restore CS0162 // Unreachable code detected
        });

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Simulated exception");

        // Verify no entities were added due to rollback
        var finalCount = await this.repository.CountAsync();
        finalCount.Value.Should().Be(initialCount.Value);
    }

    [Fact]
    public async Task TransactionAsync_WithNestedFailure_ShouldRollbackAllChanges()
    {
        // Act
        var result = await this.repository.TransactionAsync(async repo =>
        {
            // First operation - should succeed
            var entity1 = CreateTestEntity("First Entity");
            var addResult1 = await repo.AddAsync(entity1);

            if (addResult1.IsFailure)
                return Result<string>.Failure("Failed to add first entity");

            // Second operation - should succeed
            var entity2 = CreateTestEntity("Second Entity");
            var addResult2 = await repo.AddAsync(entity2);

            if (addResult2.IsFailure)
                return Result<string>.Failure("Failed to add second entity");

            // Third operation - simulate failure
            var entity3 = CreateTestEntity("Third Entity");
            var addResult3 = await repo.AddAsync(entity3);

            if (addResult3.IsSuccess)
            {
                // Simulate a late validation failure
                return Result<string>.Failure("Third entity validation failed");
            }

            return Result<string>.Success("All operations completed");
        });

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Third entity validation failed");

        // Verify all changes were rolled back
        var allEntities = await this.repository.GetAllAsync();
        allEntities.IsSuccess.Should().BeTrue();
        allEntities.Value!.Should().BeEmpty();
    }

    #endregion

    #region Advanced Transaction Tests

    [Fact]
    public async Task TransactionAsync_WithCancellation_ShouldRollbackChanges()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        var result = await this.repository.TransactionAsync(async repo =>
        {
            var entity1 = CreateTestEntity("Entity 1");
            await repo.AddAsync(entity1, cts.Token);

            // Simulate delay to trigger cancellation
            await Task.Delay(200, cts.Token);

            var entity2 = CreateTestEntity("Entity 2");
            await repo.AddAsync(entity2, cts.Token);

            return Result<bool>.Success(true);
        }, cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();

        // Verify no entities were committed due to cancellation
        var allEntities = await this.repository.GetAllAsync();
        allEntities.IsSuccess.Should().BeTrue();
        allEntities.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task TransactionAsync_WithBulkOperations_ShouldMaintainAtomicity()
    {
        // Arrange
        var setupEntities = Enumerable.Range(1, 10)
            .Select(i => CreateTestEntity($"Setup Entity {i}", isActive: i % 2 == 0))
            .ToArray();

        await this.repository.AddRangeAsync(setupEntities);
        await this.repository.SaveChangesAsync();

        // Act
        var result = await this.repository.TransactionAsync(async repo =>
        {
            // Bulk update some entities
            var updateResult = await repo.BulkUpdateAsync(
                e => e.IsActive,
                e => new TestEntity { Id = e.Id, Name = "Bulk Updated", Category = e.Category, CreatedAt = e.CreatedAt, IsActive = e.IsActive, Price = e.Price });

            if (updateResult.IsFailure)
                return Result<int>.Failure("Bulk update failed");

            // Bulk delete some entities
            var deleteResult = await repo.BulkDeleteAsync(e => !e.IsActive);

            if (deleteResult.IsFailure)
                return Result<int>.Failure("Bulk delete failed");

            // Add new entities
            var newEntities = new[]
            {
                CreateTestEntity("New Entity 1"),
                CreateTestEntity("New Entity 2")
            };

            var addResult = await repo.AddRangeAsync(newEntities);

            if (addResult.IsFailure)
                return Result<int>.Failure("Add range failed");

            return Result<int>.Success(updateResult.Value + deleteResult.Value);
        });

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify the final state
        var allEntities = await this.repository.GetAllAsync();
        allEntities.IsSuccess.Should().BeTrue();

        var finalEntities = allEntities.Value!.ToList();
        finalEntities.Should().Contain(e => e.Name == "Bulk Updated");
        finalEntities.Should().Contain(e => e.Name == "New Entity 1");
        finalEntities.Should().Contain(e => e.Name == "New Entity 2");
        finalEntities.Should().NotContain(e => e.Name.StartsWith("Setup Entity") && !e.IsActive);
    }

    #endregion

    #region Rollback Tests

    [Fact]
    public async Task RollbackChanges_AfterAddOperations_ShouldRevertAddedEntities()
    {
        // Arrange
        var entity1 = CreateTestEntity("Entity 1");
        var entity2 = CreateTestEntity("Entity 2");

        await this.repository.AddAsync(entity1);
        await this.repository.AddAsync(entity2);

        // Verify entities are in change tracker
        var entitiesBeforeRollback = this.context.ChangeTracker.Entries<TestEntity>().Count();
        entitiesBeforeRollback.Should().Be(2);

        // Act
        var result = this.repository.RollbackChanges();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify entities were removed from change tracker
        var entitiesAfterRollback = this.context.ChangeTracker.Entries<TestEntity>().Count();
        entitiesAfterRollback.Should().Be(0);

        // Verify database is empty
        var allEntities = await this.repository.GetAllAsync();
        allEntities.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task RollbackChanges_AfterUpdateOperations_ShouldRevertToOriginalValues()
    {
        // Arrange
        var entity = CreateTestEntity("Original Name", price: 100.00m);
        await this.repository.AddAsync(entity);
        await this.repository.SaveChangesAsync();

        // Modify the entity
        entity.Name = "Modified Name";
        entity.Price = 200.00m;
        await this.repository.UpdateAsync(entity);

        // Act
        var result = this.repository.RollbackChanges();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify entity was reverted to original values
        var reloadedEntity = await this.repository.GetByIdAsync(entity.Id);
        reloadedEntity.IsSuccess.Should().BeTrue();
        reloadedEntity.Value!.Name.Should().Be("Original Name");
        reloadedEntity.Value!.Price.Should().Be(100.00m);
    }

    [Fact]
    public async Task RollbackChanges_AfterDeleteOperations_ShouldRestoreDeletedEntities()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2"),
            CreateTestEntity("Entity 3")
        };

        await this.repository.AddRangeAsync(entities);
        await this.repository.SaveChangesAsync();

        // Delete some entities
        await this.repository.DeleteAsync(entities[0]);
        await this.repository.DeleteAsync(entities[2]);

        // Act
        var result = this.repository.RollbackChanges();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify all entities are still in database
        var allEntities = await this.repository.GetAllAsync();
        allEntities.IsSuccess.Should().BeTrue();
        allEntities.Value!.Should().HaveCount(3);
        allEntities.Value!.Should().Contain(e => e.Name == "Entity 1");
        allEntities.Value!.Should().Contain(e => e.Name == "Entity 2");
        allEntities.Value!.Should().Contain(e => e.Name == "Entity 3");
    }

    [Fact]
    public async Task RollbackChanges_WithMixedOperations_ShouldRevertAllChanges()
    {
        // Arrange
        var existingEntity = CreateTestEntity("Existing Entity", price: 50.00m);
        await this.repository.AddAsync(existingEntity);
        await this.repository.SaveChangesAsync();

        // Perform mixed operations
        var newEntity = CreateTestEntity("New Entity");
        await this.repository.AddAsync(newEntity);

        existingEntity.Name = "Modified Existing";
        existingEntity.Price = 75.00m;
        await this.repository.UpdateAsync(existingEntity);

        var anotherNewEntity = CreateTestEntity("Another New Entity");
        await this.repository.AddAsync(anotherNewEntity);

        // Act
        var result = this.repository.RollbackChanges();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify only original entity remains with original values
        var allEntities = await this.repository.GetAllAsync();
        allEntities.IsSuccess.Should().BeTrue();
        allEntities.Value!.Should().HaveCount(1);
        
        var remainingEntity = allEntities.Value!.First();
        remainingEntity.Name.Should().Be("Existing Entity");
        remainingEntity.Price.Should().Be(50.00m);
    }

    #endregion

    #region Helper Methods

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

    #endregion
}