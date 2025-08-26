# SharpUtils

SharpUtils is a comprehensive set of utilities for .NET, featuring libraries for functional error handling, repository patterns, and result composition.

## Libraries

### Results

The library — [SharpUtils.Results](Results/README.md) — implements the Result pattern (Either monad), enabling explicit and safe handling of operation success and failure.

- **Package**: `SharpUtils.Results`
- **Version**: 1.0.2
- **Features**: Functional error handling, railway-oriented programming, composition primitives

### Repository

The library — [SharpUtils.Repository](Repository/README.md) — provides a comprehensive repository abstraction for data access with Entity Framework Core integration and functional error handling.

- **Package**: `SharpUtils.Repository`
- **Version**: 1.0.0
- **Features**: Generic repository pattern, Entity Framework Core integration, specification pattern, transaction support

## Documentation

### Results
- [Results Documentation (EN)](Results/README.md)
- [Results Documentation (RU)](Results/README.ru.md)

### Repository
- [Repository Documentation (EN)](Repository/README.md)
- [Repository Documentation (RU)](Repository/README.ru.md)

## Russian Version

See [README.ru.md](README.ru.md) for a description in Russian.

---

## Quick Start

### Supported Platforms
- .NET 9, 8, 7, 6
- .NET Standard 2.1

### Installation

**Results Library:**
```bash
dotnet add package SharpUtils.Results
```

**Repository Library:**
```bash
dotnet add package SharpUtils.Repository
```

### Basic Usage

**Results Example:**
```csharp
using SharpUtils.Results;

// Create results
var success = Result<int>.Success(42);
var failure = Result<int>.Failure("Something went wrong");

// Handle results functionally
var result = GetUser(id)
    .Bind(user => GetUserData(user))
    .Map(data => data.ToDto())
    .Match(
        dto => $"Success: {dto}",
        error => $"Error: {error}"
    );
```

**Repository Example:**
```csharp
using SharpUtils.Repository.Generic;

// Setup
using var repository = new EfGenericRepository<Product, int>(context);

// CRUD operations with functional error handling
var result = await repository.GetByIdAsync(1);
if (result.IsSuccess)
{
    var product = result.Value;
    product.Price = 99.99m;
    await repository.UpdateAsync(product);
    await repository.SaveChangesAsync();
}

// Pagination
var pagedResult = await repository.GetPagedWhereAsync(
    p => p.IsActive,
    pageNumber: 1,
    pageSize: 20
);
```

---

## License
MIT