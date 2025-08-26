# EfGenericRepository Test Suite

This comprehensive test suite provides unit and integration tests for the `EfGenericRepository<TEntity, TKey>` class. The tests are organized into multiple specialized test files to ensure thorough coverage of all functionality, from basic CRUD operations to complex transaction scenarios.

## Test Files Overview

### 1. RepositoryTests.cs (Main Test File) 
**Purpose:** Core functionality tests covering all basic repository operations  
**Database Provider:** InMemory Database  
**Test Categories:**
- **Constructor Tests** - Repository initialization and null parameter validation
- **Add Operations** - Entity addition, bulk addition, event triggering
- **Read Operations** - Entity retrieval by ID, filtering, pagination
- **Update Operations** - Entity updates, partial updates
- **Delete Operations** - Entity deletion, bulk deletion
- **Count and Exists** - Entity counting and existence checking
- **Bulk Operations** - Bulk update and delete operations
- **Save Operations** - Change persistence and save result validation
- **Rollback Operations** - Basic rollback functionality (InMemory compatible)

**Key Features Tested:**
- ? Synchronous and asynchronous methods
- ? Error handling for null parameters
- ? Event triggering for entity changes
- ? Pagination with various parameters
- ? Result pattern implementation
- ? Bulk update and delete operations

### 2. TransactionTests.cs (Transaction & Rollback Specialist)
**Purpose:** Comprehensive transaction and rollback testing using real database  
**Database Provider:** SQLite (In-memory mode)  
**Test Categories:**
- **Transaction Success Tests** - Successful transaction commits
- **Transaction Failure Tests** - Transaction rollbacks on failures
- **Advanced Transaction Tests** - Cancellation, bulk operations, complex scenarios
- **Rollback Tests** - Detailed rollback behavior validation

**Key Features Tested:**
- ? **Real Transaction Support** - Uses SQLite for proper ACID transactions
- ? **Business Logic Transactions** - Multi-step operations with rollback
- ? **Exception Handling** - Automatic rollback on exceptions
- ? **Cancellation Support** - Cancellation token integration
- ? **Bulk Operations in Transactions** - Complex transactional workflows
- ? **Mixed Operations Rollback** - Add/Update/Delete rollback scenarios

**Detailed Test Methods:**
```csharp
// Transaction Success
TransactionAsync_WithSuccessfulOperation_ShouldCommitChanges
TransactionAsync_WithMultipleOperations_ShouldCommitAllChanges
TransactionAsync_WithComplexBusinessLogic_ShouldMaintainConsistency

// Transaction Failures
TransactionAsync_WithFailedOperation_ShouldRollbackChanges
TransactionAsync_WithException_ShouldRollbackChanges
TransactionAsync_WithNestedFailure_ShouldRollbackAllChanges

// Advanced Scenarios
TransactionAsync_WithCancellation_ShouldRollbackChanges
TransactionAsync_WithBulkOperations_ShouldMaintainAtomicity

// Rollback Tests
RollbackChanges_AfterAddOperations_ShouldRevertAddedEntities
RollbackChanges_AfterUpdateOperations_ShouldRevertToOriginalValues
RollbackChanges_AfterDeleteOperations_ShouldRestoreDeletedEntities
RollbackChanges_WithMixedOperations_ShouldRevertAllChanges
```

### 3. ErrorHandlingTests.cs
**Purpose:** Comprehensive error handling and edge case testing  
**Database Provider:** InMemory Database  
**Test Categories:**
- **Null Parameter Validation** - Testing all methods with null inputs
- **Invalid Input Handling** - Edge cases and boundary conditions
- **Error Message Verification** - Ensuring proper error messages
- **Pagination Error Handling** - Invalid page numbers and sizes

**Key Features Tested:**
- ? Null entity validation for all CRUD operations
- ? Null predicate validation for query operations
- ? Invalid pagination parameter handling
- ? Proper error message generation
- ? Result failure state validation

### 4. StreamingTests.cs
**Purpose:** Tests for streaming operations and large dataset handling  
**Database Provider:** InMemory Database  
**Test Categories:**
- **Stream Processing** - Batch-based entity streaming
- **Memory Efficiency** - Large dataset handling
- **Cancellation Support** - Cancellation token integration
- **Batch Size Validation** - Invalid batch size error handling

**Key Features Tested:**
- ? `GetAllAsStreamAsync` with configurable batch sizes
- ? Memory-efficient data retrieval for large datasets
- ? Cancellation token support in streaming operations
- ? Empty database stream handling
- ? Invalid batch size validation

### 5. ProjectionTests.cs
**Purpose:** Tests for projection operations and DTO mapping  
**Database Provider:** InMemory Database  
**Test Categories:**
- **Entity-to-DTO Projections** - Complex object mapping
- **Scalar Value Projections** - Single value extraction
- **Predicate-based Filtering** - Filtered projections
- **Error Handling** - Null parameter validation for projections

**Key Features Tested:**
- ? `GetProjectionAsync` with complex DTO mappings
- ? Scalar value projections (e.g., extracting just Price)
- ? Filtered projections with predicates
- ? Null parameter validation for projection operations

**Test DTO Example:**
```csharp
public class TestEntityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
```

### 6. SpecificationTests.cs
**Purpose:** Tests for specification pattern implementation  
**Database Provider:** InMemory Database  
**Test Categories:**
- **Criteria-based Retrieval** - Complex query specifications
- **Specification Composition** - Multiple criteria combination
- **Empty Result Handling** - No matches scenarios
- **Error Handling** - Null specification validation

**Key Features Tested:**
- ? `GetBySpecification` and `GetBySpecificationAsync` methods
- ? Complex criteria expressions
- ? Specification pattern implementation
- ? Multiple criteria combination
- ? Empty result handling

**Test Specification Example:**
```csharp
public class TestSpecification : ISpecification<TestEntity>
{
    public Expression<Func<TestEntity, bool>>? Criteria { get; set; }
    public IList<Expression<Func<TestEntity, object>>> Includes { get; set; }
    public Expression<Func<TestEntity, object>>? OrderBy { get; set; }
    public bool OrderByDescending { get; set; }
}
```

### 7. IntegrationTests.cs
**Purpose:** End-to-end workflow and integration testing  
**Database Provider:** InMemory Database  
**Test Categories:**
- **Complete CRUD Workflows** - Full entity lifecycle testing
- **Complex Multi-operation Scenarios** - Real-world usage patterns
- **Large Dataset Processing** - Performance and scalability testing
- **Cascading Operations** - Data integrity across operations

**Key Features Tested:**
- ? Complete Add ? Update ? Delete workflows
- ? Bulk operations with complex business logic
- ? Pagination workflows with large datasets
- ? Streaming workflows for memory efficiency
- ? Complex query combinations
- ? Cascading operations maintaining data integrity

## Test Infrastructure

### Test Entity
```csharp
/// <summary>
/// Test entity for unit testing purposes with comprehensive property set.
/// </summary>
public class TestEntity
{
    public int Id { get; set; }                    // Primary key
    public string Name { get; set; } = string.Empty;  // Required string with max length
    public bool IsActive { get; set; }             // Boolean for filtering tests
    public DateTime CreatedAt { get; set; }        // DateTime for ordering tests
    public string Category { get; set; } = string.Empty; // String for grouping tests
    public decimal Price { get; set; }             // Decimal for calculation tests
}
```

### Test DbContext
```csharp
/// <summary>
/// Test DbContext with proper entity configuration.
/// </summary>
public class TestDbContext : DbContext
{
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
```

### Multi-Target Framework Support
The test suite supports multiple .NET versions with appropriate Entity Framework versions:
- **.NET 9**: EF Core 9.0.0-preview.1.24081.2
- **.NET 8**: EF Core 8.0.2
- **.NET 7**: EF Core 7.0.16
- **.NET 6**: EF Core 6.0.27

### Database Providers Used

#### InMemory Database (Most Tests)
- **Provider:** `Microsoft.EntityFrameworkCore.InMemory`
- **Use Case:** Unit tests, fast execution, isolated test runs
- **Limitations:** No real transaction support, limited referential integrity

#### SQLite Database (Transaction Tests)
- **Provider:** `Microsoft.EntityFrameworkCore.Sqlite`
- **Use Case:** Transaction testing, rollback scenarios, real database behavior
- **Benefits:** Full ACID transaction support, real database constraints

### Testing Framework Stack
- **xUnit** - Primary testing framework with comprehensive attribute support
- **FluentAssertions** - Enhanced assertion library for readable test code
- **Moq** - Mocking framework (available for dependency injection scenarios)
- **Entity Framework Core** - Multi-version support for different .NET targets

## Test Coverage Matrix

| Feature Category | RepositoryTests | TransactionTests | ErrorHandlingTests | StreamingTests | ProjectionTests | SpecificationTests | IntegrationTests |
|------------------|----------------|------------------|--------------------|--------------|----|-------|--------|
| **Basic CRUD** | ? Complete | ? In Transactions | ? Error Cases | ? | ? | ? Query Focus | ? Workflows |
| **Async Operations** | ? All Methods | ? All Methods | ? All Methods | ? Streaming | ? Projections | ? All Methods | ? All Methods |
| **Transactions** | ? Basic Rollback | ? Complete | ? | ? | ? | ? | ? |
| **Bulk Operations** | ? Basic | ? In Transactions | ? Error Cases | ? | ? | ? | ? Workflows |
| **Pagination** | ? Complete | ? | ? Error Cases | ? | ? | ? | ? Large Datasets |
| **Streaming** | ? | ? | ? | ? Complete | ? | ? | ? Workflows |
| **Projections** | ? | ? | ? | ? | ? Complete | ? | ? |
| **Specifications** | ? | ? | ? | ? | ? | ? Complete | ? |
| **Error Handling** | ? Basic | ? Transaction Errors | ? Complete | ? Stream Errors | ? Projection Errors | ? Spec Errors | ? Workflow Errors |

## Running the Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Categories
```bash
# Run main repository functionality tests
dotnet test --filter "ClassName=RepositoryTests"

# Run transaction and rollback tests
dotnet test --filter "ClassName=TransactionTests"

# Run error handling tests
dotnet test --filter "ClassName=ErrorHandlingTests"

# Run streaming operation tests
dotnet test --filter "ClassName=StreamingTests"

# Run projection tests
dotnet test --filter "ClassName=ProjectionTests"

# Run specification pattern tests
dotnet test --filter "ClassName=SpecificationTests"

# Run integration workflow tests
dotnet test --filter "ClassName=IntegrationTests"
```

### Run Tests with Detailed Output
```bash
dotnet test --verbosity detailed
```

### Generate Code Coverage Report
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run Tests for Specific Framework
```bash
dotnet test --framework net9.0
dotnet test --framework net8.0
dotnet test --framework net7.0
dotnet test --framework net6.0
```

## Test Execution Performance

| Test File | Test Count | Avg Execution Time | Database Provider | Memory Usage |
|-----------|------------|-------------------|-------------------|--------------|
| RepositoryTests | ~45 tests | ~2-3 seconds | InMemory | Low |
| TransactionTests | ~12 tests | ~3-4 seconds | SQLite | Medium |
| ErrorHandlingTests | ~25 tests | ~1-2 seconds | InMemory | Low |
| StreamingTests | ~4 tests | ~2-3 seconds | InMemory | Medium |
| ProjectionTests | ~5 tests | ~1 second | InMemory | Low |
| SpecificationTests | ~7 tests | ~1-2 seconds | InMemory | Low |
| IntegrationTests | ~8 tests | ~3-4 seconds | InMemory | Medium |

## Advanced Testing Scenarios

### Transaction Testing Examples
```csharp
// Complex business logic transaction
var result = await repository.TransactionAsync(async repo =>
{
    var products = await repo.AddRangeAsync(newProducts);
    if (products.IsFailure) return Result<decimal>.Failure("Failed to add products");
    
    var totalValue = newProducts.Sum(p => p.Price);
    if (totalValue > 1000m)
    {
        // Apply discount to expensive products
        foreach (var product in newProducts.Where(p => p.Price > 100m))
        {
            product.Price *= 0.9m;
            await repo.UpdateAsync(product);
        }
    }
    
    return Result<decimal>.Success(totalValue);
});
```

### Streaming Operations Examples
```csharp
// Process large dataset efficiently
await foreach (var result in repository.GetAllAsStreamAsync(batchSize: 1000))
{
    if (result.IsSuccess)
    {
        await ProcessEntity(result.Value);
    }
}
```

### Projection Examples
```csharp
// Project to DTO for API responses
var customerSummary = await repository.GetProjectionAsync(
    projection: c => new CustomerSummaryDto 
    { 
        Id = c.Id, 
        Name = c.Name, 
        TotalOrders = c.Orders.Count() 
    },
    predicate: c => c.IsActive
);
```

## Best Practices Demonstrated

### 1. **Comprehensive Error Handling**
- All methods validate null parameters
- Proper error messages for different failure scenarios
- Result pattern for consistent error handling

### 2. **Async/Await Patterns**
- Proper async method implementations
- Cancellation token support where applicable
- Memory-efficient streaming operations

### 3. **Transaction Management**
- ACID transaction support with SQLite
- Automatic rollback on exceptions
- Business logic transaction boundaries

### 4. **Performance Considerations**
- Streaming for large datasets
- Efficient pagination implementation
- Bulk operations for multiple entities

### 5. **Testability**
- Isolated test execution
- Proper setup and teardown
- Clear test naming conventions

## Future Enhancements

### Planned Improvements
1. **Performance Benchmarks** - Add benchmarking tests for large datasets
2. **Concurrency Tests** - Test thread safety and concurrent operations  
3. **TestContainers Integration** - Test against real database systems
4. **Property-Based Testing** - Use tools like FsCheck for property-based testing
5. **Load Testing** - Stress testing with high-volume operations

### Advanced Database Testing
```csharp
// Future: TestContainers example
var container = new PostgreSqlBuilder()
    .WithDatabase("testdb")
    .WithUsername("test")
    .WithPassword("test")
    .Build();
```

This comprehensive test suite ensures that the EfGenericRepository implementation is robust, reliable, and ready for production use across all supported .NET frameworks.