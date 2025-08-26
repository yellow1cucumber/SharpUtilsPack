using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using SharpUtils.Repository.Generic;
using SharpUtils.Results;

namespace Repository.Tests.EfGeneric;

/// <summary>
/// Integration tests for EfGenericRepository covering real-world scenarios.
/// These tests focus on complete workflows and integration between methods.
/// </summary>
public class IntegrationTests : IDisposable
{
    private readonly TestDbContext context;
    private readonly EfGenericRepository<TestEntity, int> repository;

    public IntegrationTests()
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
    public async Task CompleteWorkflow_AddUpdateDeleteSave_ShouldWorkCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Original Entity");

        // Act & Assert - Add
        var addResult = await this.repository.AddAsync(entity);
        addResult.IsSuccess.Should().BeTrue();

        var saveResult1 = await this.repository.SaveChangesAsync();
        saveResult1.IsSuccess.Should().BeTrue();
        saveResult1.Value.Should().Be(1);

        // Act & Assert - Update
        entity.Name = "Updated Entity";
        var updateResult = await this.repository.UpdateAsync(entity);
        updateResult.IsSuccess.Should().BeTrue();

        var saveResult2 = await this.repository.SaveChangesAsync();
        saveResult2.IsSuccess.Should().BeTrue();
        saveResult2.Value.Should().Be(1);

        // Verify update
        var getResult = await this.repository.GetByIdAsync(entity.Id);
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value!.Name.Should().Be("Updated Entity");

        // Act & Assert - Delete
        var deleteResult = await this.repository.DeleteAsync(entity);
        deleteResult.IsSuccess.Should().BeTrue();

        var saveResult3 = await this.repository.SaveChangesAsync();
        saveResult3.IsSuccess.Should().BeTrue();
        saveResult3.Value.Should().Be(1);

        // Verify deletion
        var getAfterDeleteResult = await this.repository.GetByIdAsync(entity.Id);
        getAfterDeleteResult.IsSuccess.Should().BeTrue();
        getAfterDeleteResult.Value.Should().BeNull();
    }

    [Fact]
    public async Task BulkOperationsWorkflow_ShouldProcessMultipleEntitiesCorrectly()
    {
        // Arrange
        var entities = Enumerable.Range(1, 10)
            .Select(i => CreateTestEntity($"Entity {i}", isActive: i % 2 == 0))
            .ToArray();

        // Act & Assert - Bulk Add
        var addRangeResult = await this.repository.AddRangeAsync(entities);
        addRangeResult.IsSuccess.Should().BeTrue();
        addRangeResult.Value!.Count().Should().Be(10);

        var saveResult1 = await this.repository.SaveChangesAsync();
        saveResult1.IsSuccess.Should().BeTrue();
        saveResult1.Value.Should().Be(10);

        // Act & Assert - Individual updates (since bulk update has limitations in InMemory)
        var activeEntities = await this.repository.GetWhereAsync(e => e.IsActive);
        foreach (var entity in activeEntities.Value!)
        {
            entity.Name = "Updated Entity";
            await this.repository.UpdateAsync(entity);
        }

        var saveResult2 = await this.repository.SaveChangesAsync();
        saveResult2.IsSuccess.Should().BeTrue();

        // Verify updates
        var updatedEntities = await this.repository.GetWhereAsync(e => e.IsActive);
        updatedEntities.Value!.Should().AllSatisfy(e => e.Name.Should().Be("Updated Entity"));

        // Act & Assert - Bulk Delete (delete inactive entities)
        var bulkDeleteResult = await this.repository.BulkDeleteAsync(e => !e.IsActive);
        bulkDeleteResult.IsSuccess.Should().BeTrue();
        bulkDeleteResult.Value.Should().Be(5); // 5 inactive entities

        var saveResult3 = await this.repository.SaveChangesAsync();
        saveResult3.IsSuccess.Should().BeTrue();

        // Verify final count
        var finalCount = await this.repository.CountAsync();
        finalCount.Value.Should().Be(5); // Only active entities remain
    }

    [Fact]
    public async Task PaginationWorkflow_ShouldHandleLargeDatasetCorrectly()
    {
        // Arrange - Create a large dataset
        var entities = Enumerable.Range(1, 100)
            .Select(i => CreateTestEntity($"Entity {i:000}", price: i * 10m))
            .ToArray();

        await this.repository.AddRangeAsync(entities);
        await this.repository.SaveChangesAsync();

        // Act & Assert - Test pagination through all data
        var allEntitiesPaginated = new List<TestEntity>();
        var currentPage = 1;
        const int pageSize = 15;

        PaginatedResult<TestEntity> pageResult;
        do
        {
            pageResult = await this.repository.GetPagedAsync(currentPage, pageSize);
            pageResult.IsSuccess.Should().BeTrue();
            
            if (pageResult.Value!.Any())
            {
                allEntitiesPaginated.AddRange(pageResult.Value!);
            }
            
            currentPage++;
        } while (pageResult.Value!.Any() && currentPage <= pageResult.TotalPages);

        // Assert - All entities retrieved through pagination
        allEntitiesPaginated.Should().HaveCount(100);
        pageResult.TotalItems.Should().Be(100);
        pageResult.TotalPages.Should().Be(7); // Math.Ceiling(100/15) = 7

        // Test ordered pagination
        var orderedPageResult = await this.repository.GetPagedOrderedAsync(
            e => e.Id, 
            pageNumber: 2, 
            pageSize: 10, 
            ascending: false);

        orderedPageResult.IsSuccess.Should().BeTrue();
        orderedPageResult.Value!.Should().BeInDescendingOrder(e => e.Id);
        orderedPageResult.Value!.Count().Should().Be(10);
    }

    [Fact]
    public async Task StreamingWorkflow_ShouldHandleLargeDatasetEfficiently()
    {
        // Arrange - Create a large dataset
        var entities = Enumerable.Range(1, 1000)
            .Select(i => CreateTestEntity($"Streaming Entity {i}"))
            .ToArray();

        await this.repository.AddRangeAsync(entities);
        await this.repository.SaveChangesAsync();

        // Act - Stream all entities
        var streamedEntities = new List<TestEntity>();
        var processedBatches = 0;
        var currentBatchSize = 0;

        await foreach (var result in this.repository.GetAllAsStreamAsync(batchSize: 100))
        {
            result.IsSuccess.Should().BeTrue();
            streamedEntities.Add(result.Value!);
            
            currentBatchSize++;
            if (currentBatchSize == 100)
            {
                processedBatches++;
                currentBatchSize = 0;
            }
        }

        // Assert
        streamedEntities.Should().HaveCount(1000);
        processedBatches.Should().Be(10); // 1000 / 100 = 10 complete batches
    }

    [Fact]
    public async Task ComplexQueryWorkflow_ShouldCombineMultipleOperations()
    {
        // Arrange - Create diverse test data
        var categories = new[] { "Electronics", "Clothing", "Books", "Home" };
        var entities = new List<TestEntity>();

        for (int i = 1; i <= 40; i++)
        {
            entities.Add(CreateTestEntity(
                name: $"Product {i}",
                isActive: i % 3 != 0, // 2/3 active, 1/3 inactive
                price: i * 5m,
                category: categories[(i - 1) % categories.Length])); // Adjusted to be 0-based
        }

        await this.repository.AddRangeAsync(entities);
        await this.repository.SaveChangesAsync();

        // Act & Assert - Complex queries
        
        // 1. Count by category
        var electronicsCount = await this.repository.CountWhereAsync(e => e.Category == "Electronics");
        electronicsCount.Value.Should().Be(10); // Every 4th item

        // 2. Get expensive active products
        var expensiveProducts = await this.repository.GetWhereAsync(e => e.IsActive && e.Price > 100m);
        expensiveProducts.IsSuccess.Should().BeTrue();
        expensiveProducts.Value!.Should().AllSatisfy(e =>
        {
            e.IsActive.Should().BeTrue();
            e.Price.Should().BeGreaterThan(100m);
        });

        // 3. Paginated query with filtering
        var clothingPage = await this.repository.GetPagedWhereAsync(
            e => e.Category == "Clothing" && e.IsActive,
            pageNumber: 1,
            pageSize: 5);

        clothingPage.IsSuccess.Should().BeTrue();
        clothingPage.Value!.Should().AllSatisfy(e =>
        {
            e.Category.Should().Be("Clothing");
            e.IsActive.Should().BeTrue();
        });

        // 4. Exists check
        var hasExpensiveBooks = await this.repository.ExistsAsync(e => e.Category == "Books" && e.Price > 150m);
        hasExpensiveBooks.Value.Should().BeTrue();

        // 5. Individual updates for specific category (since bulk update has limitations)
        var homeItems = await this.repository.GetWhereAsync(e => e.Category == "Home");
        foreach (var item in homeItems.Value!)
        {
            item.Name = "Home Item - Updated";
            await this.repository.UpdateAsync(item);
        }

        await this.repository.SaveChangesAsync();

        // Verify updates
        var updatedHomeItems = await this.repository.GetWhereAsync(e => e.Category == "Home");
        updatedHomeItems.Value!.Should().AllSatisfy(e => e.Name.Should().Be("Home Item - Updated"));
    }

    [Fact]
    public async Task RollbackChangesWorkflow_ShouldRevertPendingChanges()
    {
        // Arrange
        var entity = CreateTestEntity("Original Entity");
        await this.repository.AddAsync(entity);
        await this.repository.SaveChangesAsync();

        var originalName = entity.Name;

        // Act - Make changes but don't save
        entity.Name = "Modified Entity";
        await this.repository.UpdateAsync(entity);

        var newEntity = CreateTestEntity("New Entity");
        await this.repository.AddAsync(newEntity);

        // Rollback changes
        var rollbackResult = this.repository.RollbackChanges();
        rollbackResult.IsSuccess.Should().BeTrue();

        // Assert - Changes should be reverted
        await this.context.Entry(entity).ReloadAsync();
        entity.Name.Should().Be(originalName);

        var count = await this.repository.CountAsync();
        count.Value.Should().Be(1); // Only the original entity
    }

    [Fact]
    public async Task CascadingOperations_ShouldMaintainDataIntegrity()
    {
        // Arrange - Add initial data
        var entities = Enumerable.Range(1, 20)
            .Select(i => CreateTestEntity($"Entity {i}", isActive: i <= 10))
            .ToArray();

        await this.repository.AddRangeAsync(entities);
        await this.repository.SaveChangesAsync();

        // Act & Assert - Multiple cascading operations
        
        // 1. Count all entities
        var totalCount = await this.repository.CountAsync();
        totalCount.Value.Should().Be(20);

        // 2. Count active entities
        var activeCount = await this.repository.CountWhereAsync(e => e.IsActive);
        activeCount.Value.Should().Be(10);

        // 3. Update all inactive entities to be expensive
        var inactiveEntities = await this.repository.GetWhereAsync(e => !e.IsActive);
        foreach (var entity in inactiveEntities.Value!)
        {
            entity.Price = 999.99m;
            await this.repository.UpdateAsync(entity);
        }
        await this.repository.SaveChangesAsync();

        // 4. Verify expensive entities exist
        var expensiveExists = await this.repository.ExistsAsync(e => e.Price > 500m);
        expensiveExists.Value.Should().BeTrue();

        // 5. Delete all expensive entities
        await this.repository.DeleteWhereAsync(e => e.Price > 500m);
        await this.repository.SaveChangesAsync();

        // 6. Verify final state
        var finalCount = await this.repository.CountAsync();
        finalCount.Value.Should().Be(10); // Only active entities remain

        var remainingEntities = await this.repository.GetAllAsync();
        remainingEntities.Value!.Should().AllSatisfy(e => e.IsActive.Should().BeTrue());
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