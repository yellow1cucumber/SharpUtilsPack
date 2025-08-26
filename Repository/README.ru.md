# SharpUtils.Repository - ������������ ����������

[???? English version](README.md)

[![NuGet Version](https://img.shields.io/nuget/v/SharpUtils.Repository.svg)](https://www.nuget.org/packages/SharpUtils.Repository/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SharpUtils.Repository.svg)](https://www.nuget.org/packages/SharpUtils.Repository/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

������������� ���������� ���������� ����������� ��� .NET, ��������������� �������� �������� ������� � ������ � �������������� ���������� ������ � ����������� � Entity Framework Core.

## ?? ����������

- [���������](#���������)
- [�������� ����������](#��������-����������)
- [���������� API](#����������-api)
- [������� �������������](#�������-�������������)
- [����������� �����������](#�����������-�����������)
- [������ ��������](#������-��������)
- [����������� �� ��������](#�����������-��-��������)

## ?? ���������

```bash
dotnet add package SharpUtils.Repository
```

**�������������� ����������:**
- .NET 9.0
- .NET 8.0 
- .NET 7.0
- .NET 6.0
- .NET Standard 2.1

## ??? �������� ����������

### ����������

| ��������� | �������� |
|-----------|----------|
| `IGenericRepository<TEntity, TKey>` | �������� ��������� ����������� � ������� CRUD ���������� |
| `ISpecification<T>` | ������� ������������ �������� ��� ������� �������� |

### ������

| ����� | �������� |
|-------|----------|
| `EfGenericRepository<TEntity, TKey>` | ���������� ��� Entity Framework Core |
| `EntityChangedEventArgs<TEntity>` | ��������� ������� ��� ��������� �������� |

### ������������

| ������������ | �������� |
|--------------|----------|
| `EntityChangeType` | ���� ��������� �������� (Added, Modified, Deleted) |

## ?? ���������� API

### IGenericRepository<TEntity, TKey>

�������� ��������� �����������, ��������������� ������������� �������� ������� � ������.

#### ?? �������� ��������

| ����� | ������������ ��� | �������� |
|-------|------------------|----------|
| `Add(TEntity entity)` | `Result<TEntity>` | ��������� ���� �������� |
| `AddRange(IEnumerable<TEntity> entities)` | `Result<IEnumerable<TEntity>>` | ��������� ��������� ��������� |
| `AddAsync(TEntity entity, CancellationToken)` | `Task<Result<TEntity>>` | ���������� ��������� �������� |
| `AddRangeAsync(IEnumerable<TEntity>, CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | ���������� ��������� ��������� ��������� |

#### ?? �������� ������

##### ��������� ����� ��������

| ����� | ������������ ��� | �������� |
|-------|------------------|----------|
| `GetById(TKey id)` | `Result<TEntity>` | �������� �������� �� ���������� ����� |
| `GetByIdAsync(TKey id, CancellationToken)` | `Task<Result<TEntity>>` | ���������� �������� �������� �� ����� |
| `GetFirstOrDefault(Expression<Func<TEntity, bool>>)` | `Result<TEntity>` | �������� ������ ��������, ��������������� ��������� |
| `GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<TEntity>>` | ���������� �������� ������ ��������������� �������� |

##### ��������� ���������� ���������

| ����� | ������������ ��� | �������� |
|-------|------------------|----------|
| `GetAll()` | `Result<IEnumerable<TEntity>>` | �������� ��� �������� |
| `GetAllAsync(CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | ���������� �������� ��� �������� |
| `GetWhere(Expression<Func<TEntity, bool>>)` | `Result<IEnumerable<TEntity>>` | �������� ��������, ��������������� ��������� |
| `GetWhereAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | ���������� �������� ��������������� �������� |

##### � ����������� (������ ��������)

| ����� | ������������ ��� | �������� |
|-------|------------------|----------|
| `GetWithInclude(params Expression<Func<TEntity, object>>[])` | `Result<IEnumerable<TEntity>>` | �������� �������� � ����������� ���������� |
| `GetWithIncludeAsync(CancellationToken, params Expression<Func<TEntity, object>>[])` | `Task<Result<IEnumerable<TEntity>>>` | ���������� �������� �������� � ����������� |
| `GetWhereWithInclude(Expression<Func<TEntity, bool>>, params Expression<Func<TEntity, object>>[])` | `Result<IEnumerable<TEntity>>` | �������� ��������������� �������� � ����������� |
| `GetWhereWithIncludeAsync(Expression<Func<TEntity, bool>>, CancellationToken, params Expression<Func<TEntity, object>>[])` | `Task<Result<IEnumerable<TEntity>>>` | ���������� �������� ��������������� �������� � ����������� |

##### ���������

| ����� | ������������ ��� | �������� |
|-------|------------------|----------|
| `GetPaged(int pageNumber, int pageSize)` | `PaginatedResult<TEntity>` | �������� ���������� � ���������� |
| `GetPagedAsync(int pageNumber, int pageSize, CancellationToken)` | `Task<PaginatedResult<TEntity>>` | ���������� �������� ���������� � ���������� |
| `GetPagedWhere(Expression<Func<TEntity, bool>>, int, int)` | `PaginatedResult<TEntity>` | �������� ��������������� ���������� � ���������� |
| `GetPagedWhereAsync(Expression<Func<TEntity, bool>>, int, int, CancellationToken)` | `Task<PaginatedResult<TEntity>>` | ���������� �������� ��������������� ���������� � ���������� |
| `GetPagedOrdered(Expression<Func<TEntity, TKey>>, int, int, bool)` | `PaginatedResult<TEntity>` | �������� ������������� ���������� � ���������� |
| `GetPagedOrderedAsync(Expression<Func<TEntity, TKey>>, int, int, bool, CancellationToken)` | `Task<PaginatedResult<TEntity>>` | ���������� �������� ������������� ���������� � ���������� |
| `GetPagedWhereWithInclude(Expression<Func<TEntity, bool>>, int, int, params Expression<Func<TEntity, object>>[])` | `PaginatedResult<TEntity>` | �������� ��������������� ���������� � ���������� � ����������� |
| `GetPagedWhereWithIncludeAsync(Expression<Func<TEntity, bool>>, int, int, CancellationToken, params Expression<Func<TEntity, object>>[])` | `Task<PaginatedResult<TEntity>>` | ���������� �������� ��������������� ���������� � ���������� � ����������� |
| `GetPagedOrderedWithInclude(Expression<Func<TEntity, TKey>>, int, int, bool, params Expression<Func<TEntity, object>>[])` | `PaginatedResult<TEntity>` | �������� ������������� ���������� � ���������� � ����������� |
| `GetPagedOrderedWithIncludeAsync(Expression<Func<TEntity, TKey>>, int, int, bool, CancellationToken, params Expression<Func<TEntity, object>>[])` | `Task<PaginatedResult<TEntity>>` | ���������� �������� ������������� ���������� � ���������� � ����������� |

##### ������� � �������� �������������

| ����� | ������������ ��� | �������� |
|-------|------------------|----------|
| `Count()` | `Result<int>` | �������� ����� ���������� ��������� |
| `CountAsync(CancellationToken)` | `Task<Result<int>>` | ���������� �������� ����� ���������� ��������� |
| `CountWhere(Expression<Func<TEntity, bool>>)` | `Result<int>` | �������� ���������� ��������������� ��������� |
| `CountWhereAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<int>>` | ���������� �������� ���������� ��������������� ��������� |
| `Exists(Expression<Func<TEntity, bool>>)` | `Result<bool>` | ���������, ���������� �� ��������������� �������� |
| `ExistsAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<bool>>` | ���������� ���������, ���������� �� ��������������� �������� |
| `ExistsById(TKey id)` | `Result<bool>` | ���������, ���������� �� �������� �� ����� |
| `ExistsByIdAsync(TKey id, CancellationToken)` | `Task<Result<bool>>` | ���������� ���������, ���������� �� �������� �� ����� |

#### ?? �������� ����������

| ����� | ������������ ��� | �������� |
|-------|------------------|----------|
| `Update(TEntity entity)` | `Result<TEntity>` | ��������� ���� �������� |
| `UpdateAsync(TEntity entity, CancellationToken)` | `Task<Result<TEntity>>` | ���������� ��������� �������� |
| `UpdateRange(IEnumerable<TEntity> entities)` | `Result<IEnumerable<TEntity>>` | ��������� ��������� ��������� |
| `UpdateRangeAsync(IEnumerable<TEntity>, CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | ���������� ��������� ��������� ��������� |
| `UpdatePartial<TProperty>(TEntity, Expression<Func<TEntity, TProperty>>, TProperty)` | `Result<TEntity>` | ��������� ���������� �������� |

#### ??? �������� ��������

| ����� | ������������ ��� | �������� |
|-------|------------------|----------|
| `Delete(TEntity entity)` | `Result` | ������� ���� �������� |
| `DeleteAsync(TEntity entity, CancellationToken)` | `Task<Result>` | ���������� ������� �������� |
| `DeleteRange(IEnumerable<TEntity> entities)` | `Result` | ������� ��������� ��������� |
| `DeleteRangeAsync(IEnumerable<TEntity>, CancellationToken)` | `Task<Result>` | ���������� ������� ��������� ��������� |
| `DeleteById(TKey id)` | `Result` | ������� �������� �� ����� |
| `DeleteByIdAsync(TKey id, CancellationToken)` | `Task<Result>` | ���������� ������� �������� �� ����� |
| `DeleteWhere(Expression<Func<TEntity, bool>>)` | `Result` | ������� ��������, ��������������� ��������� |
| `DeleteWhereAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result>` | ���������� ������� ��������������� �������� |

#### ? �������� ��������

| ����� | ������������ ��� | �������� |
|-------|------------------|----------|
| `BulkUpdate(Expression<Func<TEntity, bool>>, Expression<Func<TEntity, TEntity>>)` | `Result<int>` | ������� ��������� �������� |
| `BulkUpdateAsync(Expression<Func<TEntity, bool>>, Expression<Func<TEntity, TEntity>>, CancellationToken)` | `Task<Result<int>>` | ���������� ������� ��������� �������� |
| `BulkDelete(Expression<Func<TEntity, bool>>)` | `Result<int>` | ������� ������� �������� |
| `BulkDeleteAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<int>>` | ���������� ������� ������� �������� |

#### ?? �������� ����������

| ����� | ������������ ��� | �������� |
|-------|------------------|----------|
| `SaveChanges()` | `Result<int>` | ��������� ��� ��������� ��������� |
| `SaveChangesAsync(CancellationToken)` | `Task<Result<int>>` | ���������� ��������� ��������� |

#### ?? �������������� ��������

| ����� | ������������ ��� | �������� |
|-------|------------------|----------|
| `TransactionAsync<TResult>(Func<IGenericRepository<TEntity, TKey>, Task<Result<TResult>>>, CancellationToken)` | `Task<Result<TResult>>` | ��������� �������� � ���������� |
| `RollbackChanges()` | `Result` | ���������� ��������� ��������� |

#### ?? ����������� ��������

| ����� | ������������ ��� | �������� |
|-------|------------------|----------|
| `GetProjectionAsync<TProjection>(Expression<Func<TEntity, TProjection>>, Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<TProjection>>` | ���������� �������� � DTO |
| `GetAllAsStreamAsync(int batchSize, CancellationToken)` | `IAsyncEnumerable<Result<TEntity>>` | �������� �������� �������� �������� |
| `GetBySpecification(ISpecification<TEntity>)` | `Result<IEnumerable<TEntity>>` | �������� �������� �� ������������ |
| `GetBySpecificationAsync(ISpecification<TEntity>, CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | ���������� �������� �������� �� ������������ |

#### ?? ������� � ��������

| ���� | ��� | �������� |
|------|-----|----------|
| `EntityChanged` | `event EventHandler<EntityChangedEventArgs<TEntity>>` | ����������� ��� ��������� �������� |
| `Query` | `IQueryable<TEntity>` | ������ ������ � �������� (����������� � �������������) |

### ISpecification<T>

��������� ������������ �������� ��� �������� �������, ���������������� ��������.

| �������� | ��� | �������� |
|----------|-----|----------|
| `Criteria` | `Expression<Func<T, bool>>?` | �������� ���������� |
| `Includes` | `IList<Expression<Func<T, object>>>` | ��������� ��������� ��� ������ �������� |
| `OrderBy` | `Expression<Func<T, object>>?` | ��������� �������������� |
| `OrderByDescending` | `bool` | ������������� �� �� �������� |

### EntityChangedEventArgs<TEntity>

��������� ������� ��� ����������� �� ��������� ��������.

| �������� | ��� | �������� |
|----------|-----|----------|
| `Entity` | `TEntity` | ���������� �������� |
| `ChangeType` | `EntityChangeType` | ��� ��������� |
| `Timestamp` | `DateTime` | ����� ��������� ��������� |

### EntityChangeType

������������ ����� ��������� ��������.

| �������� | �������� |
|----------|----------|
| `Added` | �������� ���� ��������� |
| `Modified` | �������� ���� �������� |
| `Deleted` | �������� ���� ������� |

## ?? ������� �������������

### ������� ���������

```csharp
// ���������� ���� ��������
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

// ��������� DbContext
public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    
    // ������������...
}

// �������� �����������
using var context = new AppDbContext();
using var repository = new EfGenericRepository<Product, int>(context);
```

### CRUD ��������

```csharp
// ��������
var product = new Product { Name = "�������", Price = 999.99m, IsActive = true };
var addResult = await repository.AddAsync(product);
if (addResult.IsSuccess)
{
    await repository.SaveChangesAsync();
}

// ������
var getResult = await repository.GetByIdAsync(1);
if (getResult.IsSuccess)
{
    var foundProduct = getResult.Value;
}

// ����������
product.Price = 899.99m;
var updateResult = await repository.UpdateAsync(product);
await repository.SaveChangesAsync();

// ��������
var deleteResult = await repository.DeleteByIdAsync(1);
await repository.SaveChangesAsync();
```

### ������ ���������

```csharp
var pagedResult = await repository.GetPagedWhereAsync(
    p => p.IsActive,
    pageNumber: 1,
    pageSize: 10
);

if (pagedResult.IsSuccess)
{
    Console.WriteLine($"�������� {pagedResult.Page} �� {pagedResult.TotalPages}");
    Console.WriteLine($"����� ���������: {pagedResult.TotalItems}");
    
    foreach (var item in pagedResult.Value!)
    {
        Console.WriteLine($"- {item.Name}: {item.Price}?");
    }
}
```

### ������ �������� ������������

```csharp
public class ActiveProductsSpec : ISpecification<Product>
{
    public Expression<Func<Product, bool>>? Criteria => p => p.IsActive && p.Price > 100;
    public IList<Expression<Func<Product, object>>> Includes { get; } = 
        new List<Expression<Func<Product, object>>> { p => p.Category };
    public Expression<Func<Product, object>>? OrderBy => p => p.Name;
    public bool OrderByDescending => false;
}

// �������������
var spec = new ActiveProductsSpec();
var result = await repository.GetBySpecificationAsync(spec);
```

### ��������� ��������� ������� ������� ������

```csharp
await foreach (var result in repository.GetAllAsStreamAsync(batchSize: 1000))
{
    if (result.IsSuccess)
    {
        var product = result.Value!;
        // ��������� ������� ��������
        await ProcessProductAsync(product);
    }
}
```

### ������ ����������

```csharp
var result = await repository.TransactionAsync(async repo =>
{
    var product1 = new Product { Name = "������� 1", Price = 100m };
    var product2 = new Product { Name = "������� 2", Price = 200m };
    
    var add1 = await repo.AddAsync(product1);
    var add2 = await repo.AddAsync(product2);
    
    if (add1.IsFailure || add2.IsFailure)
        return Result<bool>.Failure("�� ������� �������� ��������");
        
    return Result<bool>.Success(true);
});
```

### ��������� �������

```csharp
repository.EntityChanged += (sender, args) =>
{
    Console.WriteLine($"�������� {args.ChangeType}: {args.Entity}");
    Console.WriteLine($"�����: {args.Timestamp}");
};
```

### �������� ��������

```csharp
// �������� ����������
var updateResult = await repository.BulkUpdateAsync(
    p => p.CategoryId == 1,
    p => new Product { IsActive = false }
);

// �������� ��������
var deleteResult = await repository.BulkDeleteAsync(p => p.IsActive == false);
```

### ������ ��������

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

## ?? ����������� �����������

### ���������� �������� Result

��� �������� ���������� `Result<T>` ��� `PaginatedResult<T>` ��� �������������� ��������� ������:

```csharp
// ����������������, ��������������� �� �������� ������
var result = await repository.GetByIdAsync(id)
    .BindAsync(async product => 
    {
        product.LastModified = DateTime.UtcNow;
        return await repository.UpdateAsync(product);
    })
    .BindAsync(_ => repository.SaveChangesAsync());

result.Match(
    success => Console.WriteLine("�������� ��������� �������"),
    error => Console.WriteLine($"�������� �� �������: {error}")
);
```

### ��������� ��������� ������������

```csharp
// Program.cs ��� Startup.cs
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddScoped<IGenericRepository<Product, int>, EfGenericRepository<Product, int>>();
services.AddScoped<IGenericRepository<Category, int>, EfGenericRepository<Category, int>>();
```

### ���������������� ������������

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

## ?? ������ ��������

### 1. ������ ����������� ����������� ������

```csharp
// ? ������
var result = await repository.GetByIdAsync(id);

// ? ���������
var result = repository.GetById(id);
```

### 2. ��������� ������������� ����������

```csharp
// ? ������
var result = await repository.GetByIdAsync(id);
if (result.IsSuccess)
{
    var product = result.Value;
    // ��������� ��������
}
else
{
    logger.LogError("�� ������� �������� �������: {Error}", result.ErrorMessage);
}

// ? ��������� ���������� ��� �������
var product = result.Value; // ����� ���� null ��� �������
```

### 3. ����������� ��������� ��� ������� ������� ������

```csharp
// ? ������ - ����������� ���������
var pagedResult = await repository.GetPagedAsync(1, 20);

// ? ��������� - �������� ���� ������
var allResult = await repository.GetAllAsync(); // ����� ��������� �������� �������
```

### 4. ����������� ������������ ��� ������� ��������

```csharp
// ? ������ - ���������������� ������������
var spec = new ActiveProductsWithCategorySpec();
var result = await repository.GetBySpecificationAsync(spec);

// ? ��������� - ������ �������������� �������
var result = await repository.GetWhereAsync(p => p.IsActive && p.Category != null);
```

### 5. ����������� ���������� ��� ��������� ��������

```csharp
// ? ������ - ��������� ��������
await repository.TransactionAsync(async repo =>
{
    await repo.AddAsync(order);
    await repo.UpdateAsync(product);
    return Result.Success();
});

// ? ��������� - ��������� �������� ��� ����������
await repository.AddAsync(order);
await repository.SaveChangesAsync();
await repository.UpdateAsync(product);
await repository.SaveChangesAsync();
```

### 6. ����������� ��������� �������� ��� ��������� ������� ������

```csharp
// ? ������ - ���������� �� ������
await foreach (var result in repository.GetAllAsStreamAsync(1000))
{
    // ��������� ��������
}

// ? ��������� - �������� ����� � ������
var all = await repository.GetAllAsync();
foreach (var item in all.Value)
{
    // ����� ������� �������� � �������
}
```

## ?? ����������� �� ��������

### �� ������� ������������� EF Core

**��:**
```csharp
var products = await context.Products
    .Where(p => p.IsActive)
    .Include(p => p.Category)
    .ToListAsync();
```

**�����:**
```csharp
var result = await repository.GetWhereWithIncludeAsync(
    p => p.IsActive,
    cancellationToken,
    p => p.Category
);

if (result.IsSuccess)
{
    var products = result.Value;
    // ������������� ���������
}
```

### �� ��������� ������ �� ������ ����������

**��:**
```csharp
try
{
    var product = await context.Products.FindAsync(id);
    if (product == null)
        throw new NotFoundException($"������� {id} �� ������");
    return product;
}
catch (Exception ex)
{
    logger.LogError(ex, "������ ��������� ��������");
    throw;
}
```

**�����:**
```csharp
var result = await repository.GetByIdAsync(id);
return result.Match(
    product => product,
    error => {
        logger.LogError("������ ��������� ��������: {Error}", error);
        return null; // ��� ����������� ��������������� �������
    }
);
```