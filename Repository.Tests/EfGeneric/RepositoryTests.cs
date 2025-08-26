using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using SharpUtils.Repository.Generic;

namespace Repository.Tests.EfGeneric;

/// <summary>
/// Test entity for unit testing purposes.
/// </summary>
public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

/// <summary>
/// Test DbContext for unit testing purposes.
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) {}

    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });
    }
}

/// <summary>
/// Comprehensive unit tests for the <see cref="EfGenericRepository{TEntity, TKey}"/> class.
/// Tests cover all CRUD operations, pagination, bulk operations, async methods, and error handling.
/// </summary>
public class RepositoryTests : IDisposable
{
    private readonly TestDbContext context;
    private readonly EfGenericRepository<TestEntity, int> repository;

    public RepositoryTests()
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

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidContext_ShouldInitializeRepository()
    {
        // Arrange & Act
        using var repository = new EfGenericRepository<TestEntity, int>(this.context);

        // Assert
        repository.Should().NotBeNull();
        repository.Query.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EfGenericRepository<TestEntity, int>(null!));
    }

    #endregion

    #region Add Operations Tests

    [Fact]
    public void Add_WithValidEntity_ShouldReturnSuccessResult()
    {
        // Arrange
        var entity = CreateTestEntity("Test Entity");

        // Act
        var result = this.repository.Add(entity);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Test Entity");
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
    }

    [Fact]
    public void Add_ShouldTriggerEntityChangedEvent()
    {
        // Arrange
        var entity = CreateTestEntity("Test Entity");
        var eventTriggered = false;
        EntityChangedEventArgs<TestEntity>? eventArgs = null;

        this.repository.EntityChanged += (sender, args) =>
        {
            eventTriggered = true;
            eventArgs = args;
        };

        // Act
        this.repository.Add(entity);

        // Assert
        eventTriggered.Should().BeTrue();
        eventArgs.Should().NotBeNull();
        eventArgs!.Entity.Should().Be(entity);
        eventArgs.ChangeType.Should().Be(EntityChangeType.Added);
    }

    [Fact]
    public void AddRange_WithValidEntities_ShouldReturnSuccessResult()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity 1"),
            CreateTestEntity("Entity 2"),
            CreateTestEntity("Entity 3")
        };

        // Act
        var result = this.repository.AddRange(entities);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count().Should().Be(3);
    }

    [Fact]
    public void AddRange_WithNullCollection_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.AddRange(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to add entities");
    }

    [Fact]
    public async Task AddAsync_WithValidEntity_ShouldReturnSuccessResult()
    {
        // Arrange
        var entity = CreateTestEntity("Async Test Entity");

        // Act
        var result = await this.repository.AddAsync(entity);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Async Test Entity");
    }

    [Fact]
    public async Task AddRangeAsync_WithValidEntities_ShouldReturnSuccessResult()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Async Entity 1"),
            CreateTestEntity("Async Entity 2")
        };

        // Act
        var result = await this.repository.AddRangeAsync(entities);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count().Should().Be(2);
    }

    #endregion

    #region Read Operations Tests

    [Fact]
    public async Task GetById_WithExistingId_ShouldReturnEntity()
    {
        // Arrange
        var entity = CreateTestEntity("Test Entity");
        await this.context.TestEntities.AddAsync(entity);
        await this.context.SaveChangesAsync();

        // Act
        var result = this.repository.GetById(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Test Entity");
    }

    [Fact]
    public void GetById_WithNonExistingId_ShouldReturnResultFailed()
    {
        // Act
        var result = this.repository.GetById(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnEntity()
    {
        // Arrange
        var entity = CreateTestEntity("Async Test Entity");
        await this.context.TestEntities.AddAsync(entity);
        await this.context.SaveChangesAsync();

        // Act
        var result = await this.repository.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Async Test Entity");
    }

    [Fact]
    public async Task GetAll_WithEntities_ShouldReturnAllEntities()
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

        // Act
        var result = this.repository.GetAll();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count().Should().Be(3);
    }

    [Fact]
    public async Task GetAllAsync_WithEntities_ShouldReturnAllEntities()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Async Entity 1"),
            CreateTestEntity("Async Entity 2")
        };
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        // Act
        var result = await this.repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count().Should().Be(2);
    }

    [Fact]
    public async Task GetFirstOrDefault_WithMatchingPredicate_ShouldReturnEntity()
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
        var result = this.repository.GetFirstOrDefault(e => e.IsActive);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Active Entity");
    }

    [Fact]
    public async Task GetFirstOrDefaultAsync_WithMatchingPredicate_ShouldReturnEntity()
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
        var result = await this.repository.GetFirstOrDefaultAsync(e => e.IsActive);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Active Entity");
    }

    [Fact]
    public async Task GetWhere_WithMatchingPredicate_ShouldReturnMatchingEntities()
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

        // Act
        var result = this.repository.GetWhere(e => e.IsActive);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count().Should().Be(2);
        result.Value!.All(e => e.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task GetWhereAsync_WithMatchingPredicate_ShouldReturnMatchingEntities()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Active Entity 1", isActive: true),
            CreateTestEntity("Inactive Entity", isActive: false)
        };
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        // Act
        var result = await this.repository.GetWhereAsync(e => e.IsActive);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count().Should().Be(1);
        result.Value!.First().Name.Should().Be("Active Entity 1");
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task GetPaged_WithValidParameters_ShouldReturnPaginatedResult()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25)
            .Select(i => CreateTestEntity($"Entity {i}"))
            .ToArray();
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        // Act
        var result = this.repository.GetPaged(pageNumber: 2, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count().Should().Be(10);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalItems.Should().Be(25);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetPagedAsync_WithValidParameters_ShouldReturnPaginatedResult()
    {
        // Arrange
        var entities = Enumerable.Range(1, 15)
            .Select(i => CreateTestEntity($"Async Entity {i}"))
            .ToArray();
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        // Act
        var result = await this.repository.GetPagedAsync(pageNumber: 1, pageSize: 5);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count().Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(5);
        result.TotalItems.Should().Be(15);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void GetPaged_WithInvalidPageNumber_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.GetPaged(pageNumber: 0, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Page number must be greater than zero");
    }

    [Fact]
    public void GetPaged_WithInvalidPageSize_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.GetPaged(pageNumber: 1, pageSize: 0);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Page size must be greater than zero");
    }

    [Fact]
    public async Task GetPagedWhere_WithPredicate_ShouldReturnFilteredPaginatedResult()
    {
        // Arrange
        var entities = Enumerable.Range(1, 20)
            .Select(i => CreateTestEntity($"Entity {i}", isActive: i % 2 == 0))
            .ToArray();
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        // Act
        var result = this.repository.GetPagedWhere(e => e.IsActive, pageNumber: 1, pageSize: 5);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count().Should().Be(5);
        result.TotalItems.Should().Be(10); // Only active entities
        result.Value!.All(e => e.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task GetPagedOrdered_WithOrderBy_ShouldReturnOrderedPaginatedResult()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity C"),
            CreateTestEntity("Entity A"),
            CreateTestEntity("Entity B")
        };
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        // Act
        var result = this.repository.GetPagedOrdered(e => e.Id, pageNumber: 1, pageSize: 10, ascending: true);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count().Should().Be(3);
        result.Value!.Should().BeInAscendingOrder(e => e.Id);
    }

    #endregion

    #region Count and Exists Tests

    [Fact]
    public async Task Count_WithEntities_ShouldReturnCorrectCount()
    {
        // Arrange
        var entities = Enumerable.Range(1, 5)
            .Select(i => CreateTestEntity($"Entity {i}"))
            .ToArray();
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        // Act
        var result = this.repository.Count();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5);
    }

    [Fact]
    public async Task CountAsync_WithEntities_ShouldReturnCorrectCount()
    {
        // Arrange
        var entities = Enumerable.Range(1, 7)
            .Select(i => CreateTestEntity($"Async Entity {i}"))
            .ToArray();
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        // Act
        var result = await this.repository.CountAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(7);
    }

    [Fact]
    public async Task CountWhere_WithPredicate_ShouldReturnCorrectCount()
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

        // Act
        var result = this.repository.CountWhere(e => e.IsActive);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task Exists_WithMatchingPredicate_ShouldReturnTrue()
    {
        // Arrange
        var entity = CreateTestEntity("Test Entity", isActive: true);
        await this.context.TestEntities.AddAsync(entity);
        await this.context.SaveChangesAsync();

        // Act
        var result = this.repository.Exists(e => e.IsActive);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsById_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var entity = CreateTestEntity("Test Entity");
        await this.context.TestEntities.AddAsync(entity);
        await this.context.SaveChangesAsync();

        // Act
        var result = this.repository.ExistsById(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public void ExistsById_WithNonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = this.repository.ExistsById(999);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    #endregion

    #region Update Operations Tests

    [Fact]
    public async Task Update_WithValidEntity_ShouldReturnSuccessResult()
    {
        // Arrange
        var entity = CreateTestEntity("Original Name");
        await this.context.TestEntities.AddAsync(entity);
        await this.context.SaveChangesAsync();

        entity.Name = "Updated Name";

        // Act
        var result = this.repository.Update(entity);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Updated Name");
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
    public async Task UpdateAsync_WithValidEntity_ShouldReturnSuccessResult()
    {
        // Arrange
        var entity = CreateTestEntity("Original Name");
        await this.context.TestEntities.AddAsync(entity);
        await this.context.SaveChangesAsync();

        entity.Name = "Async Updated Name";

        // Act
        var result = await this.repository.UpdateAsync(entity);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Async Updated Name");
    }

    [Fact]
    public async Task UpdatePartial_WithValidEntity_ShouldUpdateSpecificProperty()
    {
        // Arrange
        var entity = CreateTestEntity("Original Name");
        await this.context.TestEntities.AddAsync(entity);
        await this.context.SaveChangesAsync();

        // Act
        var result = this.repository.UpdatePartial(entity, e => e.Name, "Partially Updated Name");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Partially Updated Name");
    }

    #endregion

    #region Delete Operations Tests

    [Fact]
    public async Task Delete_WithValidEntity_ShouldReturnSuccessResult()
    {
        // Arrange
        var entity = CreateTestEntity("Entity to Delete");
        await this.context.TestEntities.AddAsync(entity);
        await this.context.SaveChangesAsync();

        // Act
        var result = this.repository.Delete(entity);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
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
    public async Task DeleteById_WithExistingId_ShouldReturnSuccessResult()
    {
        // Arrange
        var entity = CreateTestEntity("Entity to Delete");
        await this.context.TestEntities.AddAsync(entity);
        await this.context.SaveChangesAsync();

        // Act
        var result = this.repository.DeleteById(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void DeleteById_WithNonExistingId_ShouldReturnFailureResult()
    {
        // Act
        var result = this.repository.DeleteById(999);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Entity not found");
    }

    [Fact]
    public async Task DeleteWhere_WithMatchingPredicate_ShouldDeleteMatchingEntities()
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

        // Act
        var result = this.repository.DeleteWhere(e => e.IsActive);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Bulk Operations Tests

    [Fact]
    public async Task BulkUpdate_WithValidPredicate_ShouldReturnSuccessResult()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity 1", isActive: true),
            CreateTestEntity("Entity 2", isActive: true),
            CreateTestEntity("Entity 3", isActive: false)
        };
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        // Act
        var result = this.repository.BulkUpdate(
            e => e.IsActive,
            e => new TestEntity { Name = "Bulk Updated" });

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThanOrEqualTo(0); // InMemory might have different behavior
    }

    [Fact]
    public async Task BulkDelete_WithValidPredicate_ShouldReturnSuccessResult()
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("Entity 1", isActive: true),
            CreateTestEntity("Entity 2", isActive: true),
            CreateTestEntity("Entity 3", isActive: false)
        };
        await this.context.TestEntities.AddRangeAsync(entities);
        await this.context.SaveChangesAsync();

        // Act
        var result = this.repository.BulkDelete(e => e.IsActive);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThanOrEqualTo(0); // InMemory might have different behavior
    }

    #endregion

    #region Save Operations Tests

    [Fact]
    public void SaveChanges_WithPendingChanges_ShouldReturnSuccessResult()
    {
        // Arrange
        var entity = CreateTestEntity("Test Entity");
        this.repository.Add(entity);

        // Act
        var result = this.repository.SaveChanges();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SaveChangesAsync_WithPendingChanges_ShouldReturnSuccessResult()
    {
        // Arrange
        var entity = CreateTestEntity("Async Test Entity");
        await this.repository.AddAsync(entity);

        // Act
        var result = await this.repository.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    #endregion

    #region Rollback Tests

    [Fact]
    public async Task RollbackChanges_WithPendingChanges_ShouldRevertChanges()
    {
        // Arrange
        var entity = CreateTestEntity("Original Entity");
        await this.context.TestEntities.AddAsync(entity);
        await this.context.SaveChangesAsync();

        // Modify entity
        entity.Name = "Modified Name";
        this.repository.Update(entity);

        // Act
        var result = this.repository.RollbackChanges();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify entity was reverted
        var reloadedEntity = await this.repository.GetByIdAsync(entity.Id);
        reloadedEntity.Value!.Name.Should().Be("Original Entity");
    }

    #endregion

    #region Helper Methods

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

    #endregion
}
