# SharpUtils.Repository - Library Documentation

[???? Русская версия](README.ru.md)

[![NuGet Version](https://img.shields.io/nuget/v/SharpUtils.Repository.svg)](https://www.nuget.org/packages/SharpUtils.Repository/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SharpUtils.Repository.svg)](https://www.nuget.org/packages/SharpUtils.Repository/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A comprehensive repository abstraction library for .NET, providing robust data access patterns with functional error handling and Entity Framework Core integration.

## ?? Table of Contents

- [Installation](#installation)
- [Core Components](#core-components)
- [API Reference](#api-reference)
- [Usage Examples](#usage-examples)
- [Advanced Features](#advanced-features)
- [Best Practices](#best-practices)
- [Migration Guide](#migration-guide)

## ?? Installation

```bash
dotnet add package SharpUtils.Repository
```

**Supported Frameworks:**
- .NET 9.0
- .NET 8.0 
- .NET 7.0
- .NET 6.0
- .NET Standard 2.1

## ??? Core Components

### Interfaces

| Interface | Description |
|-----------|-------------|
| `IGenericRepository<TEntity, TKey>` | Main repository interface with full CRUD operations |
| `ISpecification<T>` | Query specification pattern for complex queries |

### Classes

| Class | Description |
|-------|-------------|
| `EfGenericRepository<TEntity, TKey>` | Entity Framework Core implementation |
| `EntityChangedEventArgs<TEntity>` | Event arguments for entity changes |

### Enums

| Enum | Description |
|------|-------------|
| `EntityChangeType` | Types of entity changes (Added, Modified, Deleted) |

## ?? API Reference

### IGenericRepository<TEntity, TKey>

The main repository interface providing comprehensive data access operations.

#### ?? Create Operations

| Method | Return Type | Description |
|--------|-------------|-------------|
| `Add(TEntity entity)` | `Result<TEntity>` | Adds a single entity |
| `AddRange(IEnumerable<TEntity> entities)` | `Result<IEnumerable<TEntity>>` | Adds multiple entities |
| `AddAsync(TEntity entity, CancellationToken)` | `Task<Result<TEntity>>` | Asynchronously adds entity |
| `AddRangeAsync(IEnumerable<TEntity>, CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | Asynchronously adds multiple entities |

#### ?? Read Operations

##### Single Entity Retrieval

| Method | Return Type | Description |
|--------|-------------|-------------|
| `GetById(TKey id)` | `Result<TEntity>` | Gets entity by primary key |
| `GetByIdAsync(TKey id, CancellationToken)` | `Task<Result<TEntity>>` | Asynchronously gets entity by key |
| `GetFirstOrDefault(Expression<Func<TEntity, bool>>)` | `Result<TEntity>` | Gets first entity matching predicate |
| `GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<TEntity>>` | Asynchronously gets first matching entity |

##### Multiple Entity Retrieval

| Method | Return Type | Description |
|--------|-------------|-------------|
| `GetAll()` | `Result<IEnumerable<TEntity>>` | Gets all entities |
| `GetAllAsync(CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | Asynchronously gets all entities |
| `GetWhere(Expression<Func<TEntity, bool>>)` | `Result<IEnumerable<TEntity>>` | Gets entities matching predicate |
| `GetWhereAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | Asynchronously gets matching entities |

##### With Includes (Eager Loading)

| Method | Return Type | Description |
|--------|-------------|-------------|
| `GetWithInclude(params Expression<Func<TEntity, object>>[])` | `Result<IEnumerable<TEntity>>` | Gets entities with included properties |
| `GetWithIncludeAsync(CancellationToken, params Expression<Func<TEntity, object>>[])` | `Task<Result<IEnumerable<TEntity>>>` | Asynchronously gets entities with includes |
| `GetWhereWithInclude(Expression<Func<TEntity, bool>>, params Expression<Func<TEntity, object>>[])` | `Result<IEnumerable<TEntity>>` | Gets filtered entities with includes |
| `GetWhereWithIncludeAsync(Expression<Func<TEntity, bool>>, CancellationToken, params Expression<Func<TEntity, object>>[])` | `Task<Result<IEnumerable<TEntity>>>` | Asynchronously gets filtered entities with includes |

##### Pagination

| Method | Return Type | Description |
|--------|-------------|-------------|
| `GetPaged(int pageNumber, int pageSize)` | `PaginatedResult<TEntity>` | Gets paginated results |
| `GetPagedAsync(int pageNumber, int pageSize, CancellationToken)` | `Task<PaginatedResult<TEntity>>` | Asynchronously gets paginated results |
| `GetPagedWhere(Expression<Func<TEntity, bool>>, int, int)` | `PaginatedResult<TEntity>` | Gets filtered paginated results |
| `GetPagedWhereAsync(Expression<Func<TEntity, bool>>, int, int, CancellationToken)` | `Task<PaginatedResult<TEntity>>` | Asynchronously gets filtered paginated results |
| `GetPagedOrdered(Expression<Func<TEntity, TKey>>, int, int, bool)` | `PaginatedResult<TEntity>` | Gets ordered paginated results |
| `GetPagedOrderedAsync(Expression<Func<TEntity, TKey>>, int, int, bool, CancellationToken)` | `Task<PaginatedResult<TEntity>>` | Asynchronously gets ordered paginated results |
| `GetPagedWhereWithInclude(Expression<Func<TEntity, bool>>, int, int, params Expression<Func<TEntity, object>>[])` | `PaginatedResult<TEntity>` | Gets filtered paginated results with includes |
| `GetPagedWhereWithIncludeAsync(Expression<Func<TEntity, bool>>, int, int, CancellationToken, params Expression<Func<TEntity, object>>[])` | `Task<PaginatedResult<TEntity>>` | Asynchronously gets filtered paginated results with includes |
| `GetPagedOrderedWithInclude(Expression<Func<TEntity, TKey>>, int, int, bool, params Expression<Func<TEntity, object>>[])` | `PaginatedResult<TEntity>` | Gets ordered paginated results with includes |
| `GetPagedOrderedWithIncludeAsync(Expression<Func<TEntity, TKey>>, int, int, bool, CancellationToken, params Expression<Func<TEntity, object>>[])` | `Task<PaginatedResult<TEntity>>` | Asynchronously gets ordered paginated results with includes |

##### Count and Existence

| Method | Return Type | Description |
|--------|-------------|-------------|
| `Count()` | `Result<int>` | Gets total entity count |
| `CountAsync(CancellationToken)` | `Task<Result<int>>` | Asynchronously gets total entity count |
| `CountWhere(Expression<Func<TEntity, bool>>)` | `Result<int>` | Gets count of matching entities |
| `CountWhereAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<int>>` | Asynchronously gets count of matching entities |
| `Exists(Expression<Func<TEntity, bool>>)` | `Result<bool>` | Checks if matching entity exists |
| `ExistsAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<bool>>` | Asynchronously checks if matching entity exists |
| `ExistsById(TKey id)` | `Result<bool>` | Checks if entity exists by key |
| `ExistsByIdAsync(TKey id, CancellationToken)` | `Task<Result<bool>>` | Asynchronously checks if entity exists by key |

#### ?? Update Operations

| Method | Return Type | Description |
|--------|-------------|-------------|
| `Update(TEntity entity)` | `Result<TEntity>` | Updates a single entity |
| `UpdateAsync(TEntity entity, CancellationToken)` | `Task<Result<TEntity>>` | Asynchronously updates entity |
| `UpdateRange(IEnumerable<TEntity> entities)` | `Result<IEnumerable<TEntity>>` | Updates multiple entities |
| `UpdateRangeAsync(IEnumerable<TEntity>, CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | Asynchronously updates multiple entities |
| `UpdatePartial<TProperty>(TEntity, Expression<Func<TEntity, TProperty>>, TProperty)` | `Result<TEntity>` | Updates specific property |

#### ??? Delete Operations

| Method | Return Type | Description |
|--------|-------------|-------------|
| `Delete(TEntity entity)` | `Result` | Deletes a single entity |
| `DeleteAsync(TEntity entity, CancellationToken)` | `Task<Result>` | Asynchronously deletes entity |
| `DeleteRange(IEnumerable<TEntity> entities)` | `Result` | Deletes multiple entities |
| `DeleteRangeAsync(IEnumerable<TEntity>, CancellationToken)` | `Task<Result>` | Asynchronously deletes multiple entities |
| `DeleteById(TKey id)` | `Result` | Deletes entity by key |
| `DeleteByIdAsync(TKey id, CancellationToken)` | `Task<Result>` | Asynchronously deletes entity by key |
| `DeleteWhere(Expression<Func<TEntity, bool>>)` | `Result` | Deletes entities matching predicate |
| `DeleteWhereAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result>` | Asynchronously deletes matching entities |

#### ? Bulk Operations

| Method | Return Type | Description |
|--------|-------------|-------------|
| `BulkUpdate(Expression<Func<TEntity, bool>>, Expression<Func<TEntity, TEntity>>)` | `Result<int>` | Bulk updates entities |
| `BulkUpdateAsync(Expression<Func<TEntity, bool>>, Expression<Func<TEntity, TEntity>>, CancellationToken)` | `Task<Result<int>>` | Asynchronously bulk updates entities |
| `BulkDelete(Expression<Func<TEntity, bool>>)` | `Result<int>` | Bulk deletes entities |
| `BulkDeleteAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<int>>` | Asynchronously bulk deletes entities |

#### ?? Persistence Operations

| Method | Return Type | Description |
|--------|-------------|-------------|
| `SaveChanges()` | `Result<int>` | Saves all pending changes |
| `SaveChangesAsync(CancellationToken)` | `Task<Result<int>>` | Asynchronously saves changes |

#### ?? Transaction Operations

| Method | Return Type | Description |
|--------|-------------|-------------|
| `TransactionAsync<TResult>(Func<IGenericRepository<TEntity, TKey>, Task<Result<TResult>>>, CancellationToken)` | `Task<Result<TResult>>` | Executes operation in transaction |
| `RollbackChanges()` | `Result` | Rolls back pending changes |

#### ?? Advanced Operations

| Method | Return Type | Description |
|--------|-------------|-------------|
| `GetProjectionAsync<TProjection>(Expression<Func<TEntity, TProjection>>, Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<TProjection>>` | Projects entities to DTOs |
| `GetAllAsStreamAsync(int batchSize, CancellationToken)` | `IAsyncEnumerable<Result<TEntity>>` | Streams entities in batches |
| `GetBySpecification(ISpecification<TEntity>)` | `Result<IEnumerable<TEntity>>` | Gets entities by specification |
| `GetBySpecificationAsync(ISpecification<TEntity>, CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | Asynchronously gets entities by specification |

#### ?? Events and Properties

| Member | Type | Description |
|--------|------|-------------|
| `EntityChanged` | `event EventHandler<EntityChangedEventArgs<TEntity>>` | Fired when entity changes |
| `Query` | `IQueryable<TEntity>` | Direct query access (use with caution) |

### ISpecification<T>

Query specification interface for building complex, reusable queries.

| Property | Type | Description |
|----------|------|-------------|
| `Criteria` | `Expression<Func<T, bool>>?` | Filter criteria |
| `Includes` | `IList<Expression<Func<T, object>>>` | Include expressions for eager loading |
| `OrderBy` | `Expression<Func<T, object>>?` | Ordering expression |
| `OrderByDescending` | `bool` | Whether to order descending |

### EntityChangedEventArgs<TEntity>

Event arguments for entity change notifications.

| Property | Type | Description |
|----------|------|-------------|
| `Entity` | `TEntity` | The changed entity |
| `ChangeType` | `EntityChangeType` | Type of change |
| `Timestamp` | `DateTime` | When the change occurred |

### EntityChangeType

Enumeration of entity change types.

| Value | Description |
|-------|-------------|
| `Added` | Entity was added |
| `Modified` | Entity was modified |
| `Deleted` | Entity was deleted |

## ?? Usage Examples

### Basic Setup

```csharp
// Define your entity
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

// Setup DbContext
public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    
    // Configuration...
}

// Create repository
using var context = new AppDbContext();
using var repository = new EfGenericRepository<Product, int>(context);
```

### CRUD Operations

```csharp
// Create
var product = new Product { Name = "Laptop", Price = 999.99m, IsActive = true };
var addResult = await repository.AddAsync(product);
if (addResult.IsSuccess)
{
    await repository.SaveChangesAsync();
}

// Read
var getResult = await repository.GetByIdAsync(1);
if (getResult.IsSuccess)
{
    var foundProduct = getResult.Value;
}

// Update
product.Price = 899.99m;
var updateResult = await repository.UpdateAsync(product);
await repository.SaveChangesAsync();

// Delete
var deleteResult = await repository.DeleteByIdAsync(1);
await repository.SaveChangesAsync();
```

### Pagination Example

```csharp
var pagedResult = await repository.GetPagedWhereAsync(
    p => p.IsActive,
    pageNumber: 1,
    pageSize: 10
);

if (pagedResult.IsSuccess)
{
    Console.WriteLine($"Page {pagedResult.Page} of {pagedResult.TotalPages}");
    Console.WriteLine($"Total items: {pagedResult.TotalItems}");
    
    foreach (var item in pagedResult.Value!)
    {
        Console.WriteLine($"- {item.Name}: ${item.Price}");
    }
}
```

### Specification Pattern Example

```csharp
public class ActiveProductsSpec : ISpecification<Product>
{
    public Expression<Func<Product, bool>>? Criteria => p => p.IsActive && p.Price > 100;
    public IList<Expression<Func<Product, object>>> Includes { get; } = 
        new List<Expression<Func<Product, object>>> { p => p.Category };
    public Expression<Func<Product, object>>? OrderBy => p => p.Name;
    public bool OrderByDescending => false;
}

// Usage
var spec = new ActiveProductsSpec();
var result = await repository.GetBySpecificationAsync(spec);
```

### Streaming Large Datasets

```csharp
await foreach (var result in repository.GetAllAsStreamAsync(batchSize: 1000))
{
    if (result.IsSuccess)
    {
        var product = result.Value!;
        // Process each product
        await ProcessProductAsync(product);
    }
}
```

### Transaction Example

```csharp
var result = await repository.TransactionAsync(async repo =>
{
    var product1 = new Product { Name = "Product 1", Price = 100m };
    var product2 = new Product { Name = "Product 2", Price = 200m };
    
    var add1 = await repo.AddAsync(product1);
    var add2 = await repo.AddAsync(product2);
    
    if (add1.IsFailure || add2.IsFailure)
        return Result<bool>.Failure("Failed to add products");
        
    return Result<bool>.Success(true);
});
```

### Event Handling

```csharp
repository.EntityChanged += (sender, args) =>
{
    Console.WriteLine($"Entity {args.ChangeType}: {args.Entity}");
    Console.WriteLine($"Timestamp: {args.Timestamp}");
};
```

### Bulk Operations

```csharp
// Bulk update
var updateResult = await repository.BulkUpdateAsync(
    p => p.CategoryId == 1,
    p => new Product { IsActive = false }
);

// Bulk delete
var deleteResult = await repository.BulkDeleteAsync(p => p.IsActive == false);
```

### Projection Example

```csharp
public class ProductSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
}

var projectionResult = await repository.GetProjectionAsync(
    p => new ProductSummaryDto
    {
        Id = p.Id,
        Name = p.Name,
        CategoryName = p.Category.Name
    },
    p => p.IsActive
);
```

## ?? Advanced Features

### Result Pattern Integration

All operations return `Result<T>` or `PaginatedResult<T>` for functional error handling:

```csharp
// Railway-oriented programming
var result = await repository.GetByIdAsync(id)
    .BindAsync(async product => 
    {
        product.LastModified = DateTime.UtcNow;
        return await repository.UpdateAsync(product);
    })
    .BindAsync(_ => repository.SaveChangesAsync());

result.Match(
    success => Console.WriteLine("Operation completed successfully"),
    error => Console.WriteLine($"Operation failed: {error}")
);
```

### Dependency Injection Setup

```csharp
// Program.cs or Startup.cs
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddScoped<IGenericRepository<Product, int>, EfGenericRepository<Product, int>>();
services.AddScoped<IGenericRepository<Category, int>, EfGenericRepository<Category, int>>();
```

### Custom Specifications

```csharp
public class ProductsByPriceRangeSpec : ISpecification<Product>
{
    private readonly decimal _minPrice;
    private readonly decimal _maxPrice;

    public ProductsByPriceRangeSpec(decimal minPrice, decimal maxPrice)
    {
        _minPrice = minPrice;
        _maxPrice = maxPrice;
    }

    public Expression<Func<Product, bool>>? Criteria =>
        p => p.Price >= _minPrice && p.Price <= _maxPrice && p.IsActive;

    public IList<Expression<Func<Product, object>>> Includes { get; } =
        new List<Expression<Func<Product, object>>> { p => p.Category };

    public Expression<Func<Product, object>>? OrderBy => p => p.Price;
    public bool OrderByDescending => false;
}
```

## ?? Best Practices

### 1. Always Use Async Methods

```csharp
// ? Good
var result = await repository.GetByIdAsync(id);

// ? Avoid
var result = repository.GetById(id);
```

### 2. Handle Results Properly

```csharp
// ? Good
var result = await repository.GetByIdAsync(id);
if (result.IsSuccess)
{
    var product = result.Value;
    // Process product
}
else
{
    logger.LogError("Failed to get product: {Error}", result.ErrorMessage);
}

// ? Avoid throwing on failure
var product = result.Value; // Could be null if failed
```

### 3. Use Pagination for Large Datasets

```csharp
// ? Good - Use pagination
var pagedResult = await repository.GetPagedAsync(1, 20);

// ? Avoid - Loading all data
var result = await repository.GetAllAsync(); // Could load millions of records
```

### 4. Use Specifications for Complex Queries

```csharp
// ? Good - Reusable specification
var spec = new ActiveProductsWithCategorySpec();
var result = await repository.GetBySpecificationAsync(spec);

// ? Avoid - Hardcoded queries
var result = await repository.GetWhereAsync(p => p.IsActive && p.Category != null);
```

### 5. Use Transactions for Related Operations

```csharp
// ? Good - Atomic operation
await repository.TransactionAsync(async repo =>
{
    await repo.AddAsync(order);
    await repo.UpdateAsync(product);
    return Result.Success();
});

// ? Avoid - Separate operations without transaction
await repository.AddAsync(order);
await repository.SaveChangesAsync();
await repository.UpdateAsync(product);
await repository.SaveChangesAsync();
```

### 6. Use Streaming for Large Data Processing

```csharp
// ? Good - Memory efficient
await foreach (var result in repository.GetAllAsStreamAsync(1000))
{
    // Process in batches
}

// ? Avoid - Loading all into memory
var all = await repository.GetAllAsync();
foreach (var item in all.Value)
{
    // Could cause memory issues
}
```

## ?? Migration Guide

### From Direct EF Core Usage

**Before:**
```csharp
var products = await context.Products
    .Where(p => p.IsActive)
    .Include(p => p.Category)
    .ToListAsync();
```

**After:**
```csharp
var result = await repository.GetWhereWithIncludeAsync(
    p => p.IsActive,
    cancellationToken,
    p => p.Category
);

if (result.IsSuccess)
{
    var products = result.Value;
    // Use products
}
```

### From Exception-based Error Handling

**Before:**
```csharp
try
{
    var product = await context.Products.FindAsync(id);
    if (product == null)
        throw new NotFoundException($"Product {id} not found");
    return product;
}
catch (Exception ex)
{
    logger.LogError(ex, "Error getting product");
    throw;
}
```

**After:**
```csharp
var result = await repository.GetByIdAsync(id);
return result.Match(
    product => product,
    error => {
        logger.LogError("Error getting product: {Error}", error);
        return null; // or handle appropriately
    }
);
```

## ?? Dependencies

- **SharpUtils.Results** - Functional error handling and Result pattern
- **Microsoft.EntityFrameworkCore** - ORM and database access
- **.NET Standard 2.1+** - Cross-platform compatibility

## ?? License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

## ?? Contributing

We welcome contributions! Please see [CONTRIBUTING.md](../CONTRIBUTING.md) for details.

## ?? Related Projects

- [SharpUtils.Results](../Results/README.md) - Functional error handling library

---

**SharpUtils.Repository** - *Robust data access patterns for modern .NET applications*