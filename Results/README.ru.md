# Results

������������� �������������� ���������� ����������� ��� .NET 9 � C# 13, ��������������� �������� �������� ��������� ������ � ��������� ����������. ��� ���������� �������� ��������� ��������� ������ �� ������ ���������� � ������ ����� ��������������� �������, ������� �������� ��������������, ����������� � ����� ��������.

*[English version](README.md)*

[![NuGet](https://img.shields.io/nuget/v/SharpUtils.Results.svg)](https://www.nuget.org/packages/SharpUtils.Results/)
[![Build Status](https://github.com/yellow1cucumber/UtilsPack/workflows/Build%20and%20Test/badge.svg)](https://github.com/yellow1cucumber/UtilsPack/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## �����

Results � ��� ����������� ����������, ����������� ������� Result (����� ��������� ��� ������ Either � �������������� ����������������). ��� ������������� ��������� ��������, ������� ����� ���� ������� ����������� �� ���������, ���� ����������� � ���������� �� ������, ����� ��������� ������ ����� ����� � �������������.

## ���������

���������� ����� �� NuGet:

```
dotnet add package SharpUtils.Results
```

��� ����� �������� ������� NuGet:

```
Install-Package SharpUtils.Results
```

## �������� ����������

### `Result<T>`

�������� ���������� �����, �������������� ��������� ��������:

- **��������**:
  - `Value`: �������� ���������� (��� �������� ����������)
  - `ErrorMessage`: ������ ������ (��� ����)
  - `IsSuccess`: ���������� ��������, ����������� �� �����
  - `IsFailure`: ���������� ��������, ����������� �� �������

- **��������� ������**:
  - `Success(T value)`: ������� �������� ��������� �� ���������
  - `Failure(string errorMessage)`: ������� ��������� ��������� � ���������� �� ������

- **������������� � ��������**:
  - `Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)`: ������������ ������ ������ � �������
  - `MatchAsync<TResult>(...)`: ����������� ������ Match

- **�������������**:
  - `Map<U>(Func<T, U> mapper)`: ����������� ��������, ���� ��������� �������
  - `MapAsync<U>(Func<T, Task<U>> mapper)`: ���������� ����������� ��������
  - `Bind<U>(Func<T, Result<U>> binder)`: ��������� ���������� ������
  - `BindAsync<U>(Func<T, Task<Result<U>>> binder)`: ���������� ��������� ����������

- **�������� ������**:
  - `OnSuccess(Action<T> action)`: ��������� ��������, ���� ��������� �������
  - `OnFailure(Action<string> action)`: ��������� ��������, ���� ��������� ��������

- **���������� ��������**:
  - `ValueOrDefault(T defaultValue)`: ��������� ��������� �������� ��� ���������� �������� �� ���������

- **���������� ����������**:
  - ������� �������������� �� `T` � `Result<T>`
  - ������� �������������� �� `Result<T>` � `bool` (true, ���� �������)
  - ������� �������������� �� `Result<T>` � `T` (����������� ����������, ���� �������)

### `Result` (��-����������)

������������� ��� ��������, ������� �� ���������� ��������:

- `Success()`: ������� �������� ��������� ��� ��������
- `Failure(string errorMessage)`: ������� ��������� ��������� � ���������� �� ������

### `PaginatedResult<T>`

��������� `Result<IEnumerable<T>>` ����������� ���������:

- **��������**:
  - `Page`: ������� ����� ��������
  - `PageSize`: ���������� ��������� �� ��������
  - `TotalItems`: ����� ���������� ��������� �� ���� ���������
  - `TotalPages`: ��������� ����� ���������� �������

- **��������� ������**:
  - `Success(IEnumerable<T> value, uint page, uint pageSize, uint totalItems)`
  - `Failure(string errorMessage)`
  - `Empty(uint page, uint pageSize)`

- **������������������ ������**:
  - `MapItems<U>(Func<T, U> mapper)`: ���������� ������ ������� � ���������
  - `MapItemsAsync<U>(...)`: ���������� ���������� ��������
  - `BindItems<U>(...)`: ��������� ������ ������� � ����� �����������
  - `BindItemsAsync<U>(...)`: ���������� ��������� ��������

### `Unit`

������������ ������ ��������, ������������ � ��-���������� ���������� `Result`.

## ������� �������������

### ������� ������������� Result

```csharp
using Results;

// �������� �������� � ��������� �����������
var success = Result<int>.Success(42);
var failure = Result<int>.Failure("���-�� ����� �� ���");

// �������� ������� ����������
if (success.IsSuccess)
{
    Console.WriteLine($"��������: {success.Value}");
}

// ������������� ������������� � ��������
var message = success.Match(
    value => $"�����: {value}",
    error => $"��������� ������: {error}"
);

// ������������� �������� �������
success
    .OnSuccess(value => Console.WriteLine($"�����: {value}"))
    .OnFailure(error => Console.WriteLine($"������: {error}"));

// ������������� �����������
var transformed = success
    .Map(x => x * 2)
    .Map(x => x.ToString());

// ������� ��������
var chained = success
    .Bind(x => TryDivide(100, x))
    .Bind(x => TryParse(x.ToString()));

// ������������� ���������� ����������
Result<int> implicitSuccess = 42; // ������� �������������� �� ��������
bool isSuccessful = success;      // ������� �������������� � bool
```

### ������������� PaginatedResult

```csharp
// �������� ���������� � ����������
var pagedData = PaginatedResult<string>.Success(
    new[] { "�������1", "�������2", "�������3" },
    page: 1,
    pageSize: 10,
    totalItems: 25
);

// ������ � ���������� � ���������
Console.WriteLine($"�������� {pagedData.Page} �� {pagedData.TotalPages}");
Console.WriteLine($"��������: {pagedData.Value!.Count()} �� {pagedData.TotalItems}");

// ������������� ���� ��������� � ���������
var upperCaseItems = pagedData.MapItems(item => item.ToUpper());

// �������� ��������� � ������ �����������
var boundItems = pagedData.BindItems(item => 
    item.Length > 5 
        ? Result<int>.Success(item.Length) 
        : Result<int>.Failure("������� ������� ��������")
);
```

### ����������� ��������

```csharp
// ����������� �������������
var asyncResult = await Result<int>.Success(42)
    .MapAsync(async x => {
        await Task.Delay(100);
        return x * 2;
    });

// ����������� ��������
var asyncBound = await Result<string>.Success("42")
    .BindAsync(async s => {
        await Task.Delay(100);
        if (int.TryParse(s, out var n))
            return Result<int>.Success(n);
        return Result<int>.Failure("�� �����");
    });
```

## �������� ��������� ������

### ����������������, ��������������� �� �������� ������ (Railway-Oriented Programming)

��� ���������� ������������ ������� ����������������, ��������������� �� �������� ������, ��� ������� ����� ���� ������������ ������, ������ ����������� ������:

```csharp
// ���������� ��������, ������� ���������� Results
Result<User> GetUser(string id) => /* ... */;
Result<Order> GetLatestOrder(User user) => /* ... */;
Result<Receipt> GenerateReceipt(Order order) => /* ... */;
Result<Unit> SendEmail(Receipt receipt, User user) => /* ... */;

// ������� �������� � �������������� ���������� ������
var result = GetUser("user123")
    .Bind(GetLatestOrder)
    .Bind(GenerateReceipt)
    .Bind(receipt => SendEmail(receipt, user));

// ��������� ���������� ����������
result.Match(
    _ => Console.WriteLine("������ ������� ����������"),
    error => Console.WriteLine($"������: {error}")
);
```

## ������� ����������

- **.NET 9.0**
- **.NET 8.0**
- **.NET 7.0**
- **.NET 6.0**
- **.NET Standard 2.1**
- **.NET Standard 2.0**

## ����� � ������

�� ������������ ��� ����� � ������! �� ����������� ���������� Pull Request.

1. �������� �����������
2. �������� ����� ��� ����� ������� (`git checkout -b feature/amazing-feature`)
3. ������������ ���� ��������� (`git commit -m '��������� ������������ �������'`)
4. ��������� ��������� � ����� (`git push origin feature/amazing-feature`)
5. �������� Pull Request

## ��������

���� ������ ������������ ��� ��������� MIT � ��. ���� [LICENSE](LICENSE) ��� ������������