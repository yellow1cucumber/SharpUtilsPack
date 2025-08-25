# Results

A comprehensive functional results library for .NET 9 and C# 13, providing robust error handling patterns and composition primitives. This library helps eliminate exception-based error handling in favor of a more functional approach that's type-safe, composable, and easier to reason about.

*[Russian version (Русская версия)](README.ru.md)*

[![NuGet](https://img.shields.io/nuget/v/SharpUtils.Results.svg)](https://www.nuget.org/packages/SharpUtils.Results/)
[![Build Status](https://github.com/yel/UtilsPack/workflows/Build%20and%20Test/badge.svg)](https://github.com/yellow1cucumber/UtilsPack/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Overview

Results is a lightweight library implementing the Result pattern (also known as the Either monad in functional programming). It encapsulates the outcome of an operation that can either succeed with a value or fail with an error message, making error handling more explicit and predictable.

## Installation

Install the package from NuGet:

```
dotnet add package SharpUtils.Results
```

Or via the NuGet Package Manager:

```
Install-Package SharpUtils.Results
```

## Key Components

### `Result<T>`

The core generic class representing an operation result:

- **Properties**:
  - `Value`: The result value (when successful)
  - `ErrorMessage`: The error details (when failed)
  - `IsSuccess`: Boolean indicating success
  - `IsFailure`: Boolean indicating failure

- **Factory Methods**:
  - `Success(T value)`: Creates a successful result with a value
  - `Failure(string errorMessage)`: Creates a failed result with an error message

- **Pattern Matching**:
  - `Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)`: Handles both success and failure cases
  - `MatchAsync<TResult>(...)`: Asynchronous version of Match

- **Transformation**:
  - `Map<U>(Func<T, U> mapper)`: Transforms the value if successful
  - `MapAsync<U>(Func<T, Task<U>> mapper)`: Asynchronously transforms the value
  - `Bind<U>(Func<T, Result<U>> binder)`: Chains results together
  - `BindAsync<U>(Func<T, Task<Result<U>>> binder)`: Asynchronously chains results

- **Callbacks**:
  - `OnSuccess(Action<T> action)`: Executes an action if successful
  - `OnFailure(Action<string> action)`: Executes an action if failed

- **Value Extraction**:
  - `ValueOrDefault(T defaultValue)`: Safely extracts the value or returns a default

- **Operator Overloads**:
  - Implicit conversion from `T` to `Result<T>`
  - Implicit conversion from `Result<T>` to `bool` (true if successful)
  - Implicit conversion from `Result<T>` to `T` (throws if failed)

### `Result` (Non-generic)

A specialization for operations that don't return a value:

- `Success()`: Creates a successful result with no value
- `Failure(string errorMessage)`: Creates a failed result with an error message

### `PaginatedResult<T>`

Extends `Result<IEnumerable<T>>` with pagination metadata:

- **Properties**:
  - `Page`: Current page number
  - `PageSize`: Number of items per page
  - `TotalItems`: Total number of items across all pages
  - `TotalPages`: Calculated total number of pages

- **Factory Methods**:
  - `Success(IEnumerable<T> value, uint page, uint pageSize, uint totalItems)`
  - `Failure(string errorMessage)`
  - `Empty(uint page, uint pageSize)`

- **Specialized Methods**:
  - `MapItems<U>(Func<T, U> mapper)`: Maps each item in the collection
  - `MapItemsAsync<U>(...)`: Asynchronously maps items
  - `BindItems<U>(...)`: Binds each item to a new result
  - `BindItemsAsync<U>(...)`: Asynchronously binds items

### `Unit`

Represents a void value, used in the non-generic `Result` implementation.

## Usage Examples

### Basic Result Usage

```csharp
using Results;

// Creating successful and failed results
var success = Result<int>.Success(42);
var failure = Result<int>.Failure("Something went wrong");

// Checking result status
if (success.IsSuccess)
{
    Console.WriteLine($"Value: {success.Value}");
}

// Using pattern matching
var message = success.Match(
    value => $"The answer is {value}",
    error => $"Error occurred: {error}"
);

// Using callbacks
success
    .OnSuccess(value => Console.WriteLine($"Success: {value}"))
    .OnFailure(error => Console.WriteLine($"Error: {error}"));

// Transforming results
var transformed = success
    .Map(x => x * 2)
    .Map(x => x.ToString());

// Chaining operations
var chained = success
    .Bind(x => TryDivide(100, x))
    .Bind(x => TryParse(x.ToString()));

// Using operator overloads
Result<int> implicitSuccess = 42; // Implicit conversion from value
bool isSuccessful = success;      // Implicit conversion to bool
```

### Using PaginatedResult

```csharp
// Creating a paginated result
var pagedData = PaginatedResult<string>.Success(
    new[] { "Item1", "Item2", "Item3" },
    page: 1,
    pageSize: 10,
    totalItems: 25
);

// Accessing pagination information
Console.WriteLine($"Page {pagedData.Page} of {pagedData.TotalPages}");
Console.WriteLine($"Items: {pagedData.Value!.Count()} of {pagedData.TotalItems}");

// Transforming all items in the collection
var upperCaseItems = pagedData.MapItems(item => item.ToUpper());

// Binding items to other results
var boundItems = pagedData.BindItems(item => 
    item.Length > 5 
        ? Result<int>.Success(item.Length) 
        : Result<int>.Failure("Item too short")
);
```

### Async Operations

```csharp
// Async transformations
var asyncResult = await Result<int>.Success(42)
    .MapAsync(async x => {
        await Task.Delay(100);
        return x * 2;
    });

// Async binding
var asyncBound = await Result<string>.Success("42")
    .BindAsync(async s => {
        await Task.Delay(100);
        if (int.TryParse(s, out var n))
            return Result<int>.Success(n);
        return Result<int>.Failure("Not a number");
    });
```

## Error Handling Patterns

### Railway-Oriented Programming

This library supports the railway-oriented programming pattern, where functions can be composed together while handling errors gracefully:

```csharp
// Define operations that return Results
Result<User> GetUser(string id) => /* ... */;
Result<Order> GetLatestOrder(User user) => /* ... */;
Result<Receipt> GenerateReceipt(Order order) => /* ... */;
Result<Unit> SendEmail(Receipt receipt, User user) => /* ... */;

// Chain operations, automatically handling failures
var result = GetUser("user123")
    .Bind(GetLatestOrder)
    .Bind(GenerateReceipt)
    .Bind(receipt => SendEmail(receipt, user));

// Handle the final result
result.Match(
    _ => Console.WriteLine("Email sent successfully"),
    error => Console.WriteLine($"Failed: {error}")
);
```

## Target Frameworks

- **.NET 9.0**
- **.NET 8.0**
- **.NET 7.0**
- **.NET 6.0**
- **.NET Standard 2.1**
- **.NET Standard 2.0**

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
