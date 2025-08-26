# SharpUtils

SharpUtils � ��� ������������� ����� ������ ��� .NET, ���������� ���������� ��� �������������� ��������� ������, ��������� ����������� � ���������� �����������.

## ����������

### Results

���������� � [SharpUtils.Results](Results/README.ru.md) � ��������� ������� Result (������ Either), ����������� ����� � ���������� ��������� ������ � ������� ��������.

- **�����**: `SharpUtils.Results`
- **������**: 1.0.2
- **�����������**: �������������� ��������� ������, ����������������, ��������������� �� �������� ������, ��������� ����������

### Repository

���������� � [SharpUtils.Repository](Repository/README.ru.md) � ������������� ������������� ���������� ����������� ��� ������� � ������ � ����������� Entity Framework Core � �������������� ���������� ������.

- **�����**: `SharpUtils.Repository`
- **������**: 1.0.0
- **�����������**: �������� ������������� �������� "�����������", ���������� � Entity Framework Core, ������� ������������, ��������� ����������

## ������������

### Results
- [������������ Results (RU)](Results/README.ru.md)
- [������������ Results (EN)](Results/README.md)

### Repository
- [������������ Repository (RU)](Repository/README.ru.md)
- [������������ Repository (EN)](Repository/README.md)

## English Version

��. [README.md](README.md) ��� �������� �� ���������� �����.

---

## ������� �����

### �������������� ���������
- .NET 9, 8, 7, 6
- .NET Standard 2.1

### ���������

**���������� Results:**
```bash
dotnet add package SharpUtils.Results
```

**���������� Repository:**
```bash
dotnet add package SharpUtils.Repository
```

### ������� �������������

**������ Results:**
```csharp
using SharpUtils.Results;

// �������� �����������
var success = Result<int>.Success(42);
var failure = Result<int>.Failure("���-�� ����� �� ���");

// �������������� ��������� �����������
var result = GetUser(id)
    .Bind(user => GetUserData(user))
    .Map(data => data.ToDto())
    .Match(
        dto => $"�����: {dto}",
        error => $"������: {error}"
    );
```

**������ Repository:**
```csharp
using SharpUtils.Repository.Generic;

// ���������
using var repository = new EfGenericRepository<Product, int>(context);

// CRUD �������� � �������������� ���������� ������
var result = await repository.GetByIdAsync(1);
if (result.IsSuccess)
{
    var product = result.Value;
    product.Price = 99.99m;
    await repository.UpdateAsync(product);
    await repository.SaveChangesAsync();
}

// ���������
var pagedResult = await repository.GetPagedWhereAsync(
    p => p.IsActive,
    pageNumber: 1,
    pageSize: 20
);
```

---

## ��������
MIT
