# Results

������������� �������������� ���������� ����������� ��� .NET 9 � C# 13, ��������������� �������� ������� ��������� ������ � ��������� ����������. ��� ���������� �������� ��������� ��������� ������ �� ������ ���������� � ������ ����� ��������������� �������, ������� �������� ��������������, ����������� � ����� ��������.

*[English version](README.md)*

## �����

Results � ��� ����������� ����������, ����������� ������� Result (����� ��������� ��� ������ Either � �������������� ����������������). ��� ������������� ��������� ��������, ������� ����� ���� ������� ����������� �� ���������, ���� ����������� � ���������� �� ������, ����� ��������� ������ ����� ����� � �������������.

## �������� ����������

### `Result<T>`

�������� ���������� �����, �������������� ��������� ��������:

- **��������**:
  - `Value`: �������� ���������� (��� �������� ����������)
  - `ErrorMessage`: ������ ������ (��� �������)
  - `IsSuccess`: ���������� ��������, ����������� �� �����
  - `IsFailure`: ���������� ��������, ����������� �� �������

- **��������� ������**:
  - `Success(T value)`: ������� �������� ��������� � ���������
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
  - `OnSuccess(Action<T> action)`: ��������� ��������, ���� ��������� ��������
  - `OnFailure(Action<string> action)`: ��������� ��������, ���� ��������� ���������

- **���������� ��������**:
  - `ValueOrDefault(T defaultValue)`: ��������� ��������� �������� ��� ���������� �������� �� ���������

- **���������� ����������**:
  - ������� �������������� �� `T` � `Result<T>`
  - ������� �������������� �� `Result<T>` � `bool` (true, ���� �������)
  - ������� �������������� �� `Result<T>` � `T` (�������� ����������, ���� ��������� ���������)

### `Result` (��� ����)

������������� ��� ��������, ������� �� ���������� ��������:

- `Success()`: ������� �������� ��������� ��� ��������
- `Failure(string errorMessage)`: ������� ��������� ��������� � ���������� �� ������

### `PaginatedResult<T>`

��������� `Result<IEnumerable<T>>` � ����������� ���������:

- **��������**:
  - `Page`: ������� ����� ��������
  - `PageSize`: ���������� ��������� �� ��������
  - `TotalItems`: ����� ���������� ��������� �� ���� ���������
  - `TotalPages`: ������������ ����� ���������� �������

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

������������ ������ ��������, ������������ � ���������� ����������������� `Result`.

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

// �������������� �����������
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

// �������������� ���� ��������� � ���������
var upperCaseItems = pagedData.MapItems(item => item.ToUpper());

// ���������� ��������� � ������� ������������
var boundItems = pagedData.BindItems(item => 
    item.Length > 5 
        ? Result<int>.Success(item.Length) 
        : Result<int>.Failure("������� ������� ��������")
);
```

### ����������� ��������

```csharp
// ����������� ��������������
var asyncResult = await Result<int>.Success(42)
    .MapAsync(async x => {
        await Task.Delay(100);
        return x * 2;
    });

// ����������� ����������
var asyncBound = await Result<string>.Success("42")
    .BindAsync(async s => {
        await Task.Delay(100);
        if (int.TryParse(s, out var n))
            return Result<int>.Success(n);
        return Result<int>.Failure("�� �����");
    });
```

## ������� ��������� ������

### ����������������, ��������������� �� �������� ������ (Railway-Oriented Programming)

��� ���������� ������������ ������� ����������������, ��������������� �� �������� ������, ��� ������� ����� ���� ������������ ������, ����������� ������ ������:

```csharp
// ����������� ��������, ������������ Result
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

## ������� ���������

- **.NET 9.0**
- **C# 13.0**

## ���������

�������� ������ �� ������ � ���� �������:

```xml
<ProjectReference Include="Results\Results.csproj" />
```

## ������������

������������ ��������� ����� ��������������� � ������� `Results.Tests` � �������������� xUnit.

## ��������

�������� MIT (��� ������� �������������� ��������)