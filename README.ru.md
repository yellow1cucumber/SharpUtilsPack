# SharpUtils

SharpUtils — это всеобъемлющий набор утилит для .NET, включающий библиотеки для функциональной обработки ошибок, паттернов репозитория и композиции результатов.

## Библиотеки

### Results

Библиотека — [SharpUtils.Results](Results/README.ru.md) — реализует паттерн Result (монада Either), обеспечивая явную и безопасную обработку успеха и неудачи операций.

- **Пакет**: `SharpUtils.Results`
- **Версия**: 1.0.2
- **Возможности**: Функциональная обработка ошибок, программирование, ориентированное на железную дорогу, примитивы композиции

### Repository

Библиотека — [SharpUtils.Repository](Repository/README.ru.md) — предоставляет всеобъемлющую абстракцию репозитория для доступа к данным с интеграцией Entity Framework Core и функциональной обработкой ошибок.

- **Пакет**: `SharpUtils.Repository`
- **Версия**: 1.0.0
- **Возможности**: Дженерик имплементация паттерна "репозиторий", интеграция с Entity Framework Core, паттерн спецификации, поддержка транзакций

## Документация

### Results
- [Документация Results (RU)](Results/README.ru.md)
- [Документация Results (EN)](Results/README.md)

### Repository
- [Документация Repository (RU)](Repository/README.ru.md)
- [Документация Repository (EN)](Repository/README.md)

## English Version

См. [README.md](README.md) для описания на английском языке.

---

## Быстрый старт

### Поддерживаемые платформы
- .NET 9, 8, 7, 6
- .NET Standard 2.1

### Установка

**Библиотека Results:**
```bash
dotnet add package SharpUtils.Results
```

**Библиотека Repository:**
```bash
dotnet add package SharpUtils.Repository
```

### Базовое использование

**Пример Results:**
```csharp
using SharpUtils.Results;

// Создание результатов
var success = Result<int>.Success(42);
var failure = Result<int>.Failure("Что-то пошло не так");

// Функциональная обработка результатов
var result = GetUser(id)
    .Bind(user => GetUserData(user))
    .Map(data => data.ToDto())
    .Match(
        dto => $"Успех: {dto}",
        error => $"Ошибка: {error}"
    );
```

**Пример Repository:**
```csharp
using SharpUtils.Repository.Generic;

// Настройка
using var repository = new EfGenericRepository<Product, int>(context);

// CRUD операции с функциональной обработкой ошибок
var result = await repository.GetByIdAsync(1);
if (result.IsSuccess)
{
    var product = result.Value;
    product.Price = 99.99m;
    await repository.UpdateAsync(product);
    await repository.SaveChangesAsync();
}

// Пагинация
var pagedResult = await repository.GetPagedWhereAsync(
    p => p.IsActive,
    pageNumber: 1,
    pageSize: 20
);
```

---

## Лицензия
MIT
