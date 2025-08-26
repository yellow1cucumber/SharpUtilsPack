# SharpUtils.Repository - Документация библиотеки

[???? English version](README.md)

[![NuGet Version](https://img.shields.io/nuget/v/SharpUtils.Repository.svg)](https://www.nuget.org/packages/SharpUtils.Repository/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SharpUtils.Repository.svg)](https://www.nuget.org/packages/SharpUtils.Repository/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Всеобъемлющая библиотека абстракции репозитория для .NET, предоставляющая надежные паттерны доступа к данным с функциональной обработкой ошибок и интеграцией с Entity Framework Core.

## ?? Содержание

- [Установка](#установка)
- [Основные компоненты](#основные-компоненты)
- [Справочник API](#справочник-api)
- [Примеры использования](#примеры-использования)
- [Расширенные возможности](#расширенные-возможности)
- [Лучшие практики](#лучшие-практики)
- [Руководство по миграции](#руководство-по-миграции)

## ?? Установка

```bash
dotnet add package SharpUtils.Repository
```

**Поддерживаемые фреймворки:**
- .NET 9.0
- .NET 8.0 
- .NET 7.0
- .NET 6.0
- .NET Standard 2.1

## ??? Основные компоненты

### Интерфейсы

| Интерфейс | Описание |
|-----------|----------|
| `IGenericRepository<TEntity, TKey>` | Основной интерфейс репозитория с полными CRUD операциями |
| `ISpecification<T>` | Паттерн спецификации запросов для сложных запросов |

### Классы

| Класс | Описание |
|-------|----------|
| `EfGenericRepository<TEntity, TKey>` | Реализация для Entity Framework Core |
| `EntityChangedEventArgs<TEntity>` | Аргументы события для изменений сущности |

### Перечисления

| Перечисление | Описание |
|--------------|----------|
| `EntityChangeType` | Типы изменений сущности (Added, Modified, Deleted) |

## ?? Справочник API

### IGenericRepository<TEntity, TKey>

Основной интерфейс репозитория, предоставляющий всеобъемлющие операции доступа к данным.

#### ?? Операции создания

| Метод | Возвращаемый тип | Описание |
|-------|------------------|----------|
| `Add(TEntity entity)` | `Result<TEntity>` | Добавляет одну сущность |
| `AddRange(IEnumerable<TEntity> entities)` | `Result<IEnumerable<TEntity>>` | Добавляет несколько сущностей |
| `AddAsync(TEntity entity, CancellationToken)` | `Task<Result<TEntity>>` | Асинхронно добавляет сущность |
| `AddRangeAsync(IEnumerable<TEntity>, CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | Асинхронно добавляет несколько сущностей |

#### ?? Операции чтения

##### Получение одной сущности

| Метод | Возвращаемый тип | Описание |
|-------|------------------|----------|
| `GetById(TKey id)` | `Result<TEntity>` | Получает сущность по первичному ключу |
| `GetByIdAsync(TKey id, CancellationToken)` | `Task<Result<TEntity>>` | Асинхронно получает сущность по ключу |
| `GetFirstOrDefault(Expression<Func<TEntity, bool>>)` | `Result<TEntity>` | Получает первую сущность, соответствующую предикату |
| `GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<TEntity>>` | Асинхронно получает первую соответствующую сущность |

##### Получение нескольких сущностей

| Метод | Возвращаемый тип | Описание |
|-------|------------------|----------|
| `GetAll()` | `Result<IEnumerable<TEntity>>` | Получает все сущности |
| `GetAllAsync(CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | Асинхронно получает все сущности |
| `GetWhere(Expression<Func<TEntity, bool>>)` | `Result<IEnumerable<TEntity>>` | Получает сущности, соответствующие предикату |
| `GetWhereAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | Асинхронно получает соответствующие сущности |

##### С включениями (жадная загрузка)

| Метод | Возвращаемый тип | Описание |
|-------|------------------|----------|
| `GetWithInclude(params Expression<Func<TEntity, object>>[])` | `Result<IEnumerable<TEntity>>` | Получает сущности с включенными свойствами |
| `GetWithIncludeAsync(CancellationToken, params Expression<Func<TEntity, object>>[])` | `Task<Result<IEnumerable<TEntity>>>` | Асинхронно получает сущности с включениями |
| `GetWhereWithInclude(Expression<Func<TEntity, bool>>, params Expression<Func<TEntity, object>>[])` | `Result<IEnumerable<TEntity>>` | Получает отфильтрованные сущности с включениями |
| `GetWhereWithIncludeAsync(Expression<Func<TEntity, bool>>, CancellationToken, params Expression<Func<TEntity, object>>[])` | `Task<Result<IEnumerable<TEntity>>>` | Асинхронно получает отфильтрованные сущности с включениями |

##### Пагинация

| Метод | Возвращаемый тип | Описание |
|-------|------------------|----------|
| `GetPaged(int pageNumber, int pageSize)` | `PaginatedResult<TEntity>` | Получает результаты с пагинацией |
| `GetPagedAsync(int pageNumber, int pageSize, CancellationToken)` | `Task<PaginatedResult<TEntity>>` | Асинхронно получает результаты с пагинацией |
| `GetPagedWhere(Expression<Func<TEntity, bool>>, int, int)` | `PaginatedResult<TEntity>` | Получает отфильтрованные результаты с пагинацией |
| `GetPagedWhereAsync(Expression<Func<TEntity, bool>>, int, int, CancellationToken)` | `Task<PaginatedResult<TEntity>>` | Асинхронно получает отфильтрованные результаты с пагинацией |
| `GetPagedOrdered(Expression<Func<TEntity, TKey>>, int, int, bool)` | `PaginatedResult<TEntity>` | Получает упорядоченные результаты с пагинацией |
| `GetPagedOrderedAsync(Expression<Func<TEntity, TKey>>, int, int, bool, CancellationToken)` | `Task<PaginatedResult<TEntity>>` | Асинхронно получает упорядоченные результаты с пагинацией |
| `GetPagedWhereWithInclude(Expression<Func<TEntity, bool>>, int, int, params Expression<Func<TEntity, object>>[])` | `PaginatedResult<TEntity>` | Получает отфильтрованные результаты с пагинацией и включениями |
| `GetPagedWhereWithIncludeAsync(Expression<Func<TEntity, bool>>, int, int, CancellationToken, params Expression<Func<TEntity, object>>[])` | `Task<PaginatedResult<TEntity>>` | Асинхронно получает отфильтрованные результаты с пагинацией и включениями |
| `GetPagedOrderedWithInclude(Expression<Func<TEntity, TKey>>, int, int, bool, params Expression<Func<TEntity, object>>[])` | `PaginatedResult<TEntity>` | Получает упорядоченные результаты с пагинацией и включениями |
| `GetPagedOrderedWithIncludeAsync(Expression<Func<TEntity, TKey>>, int, int, bool, CancellationToken, params Expression<Func<TEntity, object>>[])` | `Task<PaginatedResult<TEntity>>` | Асинхронно получает упорядоченные результаты с пагинацией и включениями |

##### Подсчет и проверка существования

| Метод | Возвращаемый тип | Описание |
|-------|------------------|----------|
| `Count()` | `Result<int>` | Получает общее количество сущностей |
| `CountAsync(CancellationToken)` | `Task<Result<int>>` | Асинхронно получает общее количество сущностей |
| `CountWhere(Expression<Func<TEntity, bool>>)` | `Result<int>` | Получает количество соответствующих сущностей |
| `CountWhereAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<int>>` | Асинхронно получает количество соответствующих сущностей |
| `Exists(Expression<Func<TEntity, bool>>)` | `Result<bool>` | Проверяет, существует ли соответствующая сущность |
| `ExistsAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<bool>>` | Асинхронно проверяет, существует ли соответствующая сущность |
| `ExistsById(TKey id)` | `Result<bool>` | Проверяет, существует ли сущность по ключу |
| `ExistsByIdAsync(TKey id, CancellationToken)` | `Task<Result<bool>>` | Асинхронно проверяет, существует ли сущность по ключу |

#### ?? Операции обновления

| Метод | Возвращаемый тип | Описание |
|-------|------------------|----------|
| `Update(TEntity entity)` | `Result<TEntity>` | Обновляет одну сущность |
| `UpdateAsync(TEntity entity, CancellationToken)` | `Task<Result<TEntity>>` | Асинхронно обновляет сущность |
| `UpdateRange(IEnumerable<TEntity> entities)` | `Result<IEnumerable<TEntity>>` | Обновляет несколько сущностей |
| `UpdateRangeAsync(IEnumerable<TEntity>, CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | Асинхронно обновляет несколько сущностей |
| `UpdatePartial<TProperty>(TEntity, Expression<Func<TEntity, TProperty>>, TProperty)` | `Result<TEntity>` | Обновляет конкретное свойство |

#### ??? Операции удаления

| Метод | Возвращаемый тип | Описание |
|-------|------------------|----------|
| `Delete(TEntity entity)` | `Result` | Удаляет одну сущность |
| `DeleteAsync(TEntity entity, CancellationToken)` | `Task<Result>` | Асинхронно удаляет сущность |
| `DeleteRange(IEnumerable<TEntity> entities)` | `Result` | Удаляет несколько сущностей |
| `DeleteRangeAsync(IEnumerable<TEntity>, CancellationToken)` | `Task<Result>` | Асинхронно удаляет несколько сущностей |
| `DeleteById(TKey id)` | `Result` | Удаляет сущность по ключу |
| `DeleteByIdAsync(TKey id, CancellationToken)` | `Task<Result>` | Асинхронно удаляет сущность по ключу |
| `DeleteWhere(Expression<Func<TEntity, bool>>)` | `Result` | Удаляет сущности, соответствующие предикату |
| `DeleteWhereAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result>` | Асинхронно удаляет соответствующие сущности |

#### ? Массовые операции

| Метод | Возвращаемый тип | Описание |
|-------|------------------|----------|
| `BulkUpdate(Expression<Func<TEntity, bool>>, Expression<Func<TEntity, TEntity>>)` | `Result<int>` | Массово обновляет сущности |
| `BulkUpdateAsync(Expression<Func<TEntity, bool>>, Expression<Func<TEntity, TEntity>>, CancellationToken)` | `Task<Result<int>>` | Асинхронно массово обновляет сущности |
| `BulkDelete(Expression<Func<TEntity, bool>>)` | `Result<int>` | Массово удаляет сущности |
| `BulkDeleteAsync(Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<int>>` | Асинхронно массово удаляет сущности |

#### ?? Операции сохранения

| Метод | Возвращаемый тип | Описание |
|-------|------------------|----------|
| `SaveChanges()` | `Result<int>` | Сохраняет все ожидающие изменения |
| `SaveChangesAsync(CancellationToken)` | `Task<Result<int>>` | Асинхронно сохраняет изменения |

#### ?? Транзакционные операции

| Метод | Возвращаемый тип | Описание |
|-------|------------------|----------|
| `TransactionAsync<TResult>(Func<IGenericRepository<TEntity, TKey>, Task<Result<TResult>>>, CancellationToken)` | `Task<Result<TResult>>` | Выполняет операцию в транзакции |
| `RollbackChanges()` | `Result` | Откатывает ожидающие изменения |

#### ?? Расширенные операции

| Метод | Возвращаемый тип | Описание |
|-------|------------------|----------|
| `GetProjectionAsync<TProjection>(Expression<Func<TEntity, TProjection>>, Expression<Func<TEntity, bool>>, CancellationToken)` | `Task<Result<TProjection>>` | Проецирует сущности в DTO |
| `GetAllAsStreamAsync(int batchSize, CancellationToken)` | `IAsyncEnumerable<Result<TEntity>>` | Потоково передает сущности пакетами |
| `GetBySpecification(ISpecification<TEntity>)` | `Result<IEnumerable<TEntity>>` | Получает сущности по спецификации |
| `GetBySpecificationAsync(ISpecification<TEntity>, CancellationToken)` | `Task<Result<IEnumerable<TEntity>>>` | Асинхронно получает сущности по спецификации |

#### ?? События и свойства

| Член | Тип | Описание |
|------|-----|----------|
| `EntityChanged` | `event EventHandler<EntityChangedEventArgs<TEntity>>` | Срабатывает при изменении сущности |
| `Query` | `IQueryable<TEntity>` | Прямой доступ к запросам (используйте с осторожностью) |

### ISpecification<T>

Интерфейс спецификации запросов для создания сложных, переиспользуемых запросов.

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Criteria` | `Expression<Func<T, bool>>?` | Критерии фильтрации |
| `Includes` | `IList<Expression<Func<T, object>>>` | Выражения включения для жадной загрузки |
| `OrderBy` | `Expression<Func<T, object>>?` | Выражение упорядочивания |
| `OrderByDescending` | `bool` | Упорядочивать ли по убыванию |

### EntityChangedEventArgs<TEntity>

Аргументы события для уведомлений об изменении сущности.

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Entity` | `TEntity` | Измененная сущность |
| `ChangeType` | `EntityChangeType` | Тип изменения |
| `Timestamp` | `DateTime` | Когда произошло изменение |

### EntityChangeType

Перечисление типов изменений сущности.

| Значение | Описание |
|----------|----------|
| `Added` | Сущность была добавлена |
| `Modified` | Сущность была изменена |
| `Deleted` | Сущность была удалена |

## ?? Примеры использования

### Базовая настройка

```csharp
// Определите вашу сущность
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

// Настройка DbContext
public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    
    // Конфигурация...
}

// Создание репозитория
using var context = new AppDbContext();
using var repository = new EfGenericRepository<Product, int>(context);
```

### CRUD операции

```csharp
// Создание
var product = new Product { Name = "Ноутбук", Price = 999.99m, IsActive = true };
var addResult = await repository.AddAsync(product);
if (addResult.IsSuccess)
{
    await repository.SaveChangesAsync();
}

// Чтение
var getResult = await repository.GetByIdAsync(1);
if (getResult.IsSuccess)
{
    var foundProduct = getResult.Value;
}

// Обновление
product.Price = 899.99m;
var updateResult = await repository.UpdateAsync(product);
await repository.SaveChangesAsync();

// Удаление
var deleteResult = await repository.DeleteByIdAsync(1);
await repository.SaveChangesAsync();
```

### Пример пагинации

```csharp
var pagedResult = await repository.GetPagedWhereAsync(
    p => p.IsActive,
    pageNumber: 1,
    pageSize: 10
);

if (pagedResult.IsSuccess)
{
    Console.WriteLine($"Страница {pagedResult.Page} из {pagedResult.TotalPages}");
    Console.WriteLine($"Всего элементов: {pagedResult.TotalItems}");
    
    foreach (var item in pagedResult.Value!)
    {
        Console.WriteLine($"- {item.Name}: {item.Price}?");
    }
}
```

### Пример паттерна спецификации

```csharp
public class ActiveProductsSpec : ISpecification<Product>
{
    public Expression<Func<Product, bool>>? Criteria => p => p.IsActive && p.Price > 100;
    public IList<Expression<Func<Product, object>>> Includes { get; } = 
        new List<Expression<Func<Product, object>>> { p => p.Category };
    public Expression<Func<Product, object>>? OrderBy => p => p.Name;
    public bool OrderByDescending => false;
}

// Использование
var spec = new ActiveProductsSpec();
var result = await repository.GetBySpecificationAsync(spec);
```

### Потоковая обработка больших наборов данных

```csharp
await foreach (var result in repository.GetAllAsStreamAsync(batchSize: 1000))
{
    if (result.IsSuccess)
    {
        var product = result.Value!;
        // Обработка каждого продукта
        await ProcessProductAsync(product);
    }
}
```

### Пример транзакции

```csharp
var result = await repository.TransactionAsync(async repo =>
{
    var product1 = new Product { Name = "Продукт 1", Price = 100m };
    var product2 = new Product { Name = "Продукт 2", Price = 200m };
    
    var add1 = await repo.AddAsync(product1);
    var add2 = await repo.AddAsync(product2);
    
    if (add1.IsFailure || add2.IsFailure)
        return Result<bool>.Failure("Не удалось добавить продукты");
        
    return Result<bool>.Success(true);
});
```

### Обработка событий

```csharp
repository.EntityChanged += (sender, args) =>
{
    Console.WriteLine($"Сущность {args.ChangeType}: {args.Entity}");
    Console.WriteLine($"Время: {args.Timestamp}");
};
```

### Массовые операции

```csharp
// Массовое обновление
var updateResult = await repository.BulkUpdateAsync(
    p => p.CategoryId == 1,
    p => new Product { IsActive = false }
);

// Массовое удаление
var deleteResult = await repository.BulkDeleteAsync(p => p.IsActive == false);
```

### Пример проекции

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

## ?? Расширенные возможности

### Интеграция паттерна Result

Все операции возвращают `Result<T>` или `PaginatedResult<T>` для функциональной обработки ошибок:

```csharp
// Программирование, ориентированное на железную дорогу
var result = await repository.GetByIdAsync(id)
    .BindAsync(async product => 
    {
        product.LastModified = DateTime.UtcNow;
        return await repository.UpdateAsync(product);
    })
    .BindAsync(_ => repository.SaveChangesAsync());

result.Match(
    success => Console.WriteLine("Операция выполнена успешно"),
    error => Console.WriteLine($"Операция не удалась: {error}")
);
```

### Настройка внедрения зависимостей

```csharp
// Program.cs или Startup.cs
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddScoped<IGenericRepository<Product, int>, EfGenericRepository<Product, int>>();
services.AddScoped<IGenericRepository<Category, int>, EfGenericRepository<Category, int>>();
```

### Пользовательские спецификации

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

## ?? Лучшие практики

### 1. Всегда используйте асинхронные методы

```csharp
// ? Хорошо
var result = await repository.GetByIdAsync(id);

// ? Избегайте
var result = repository.GetById(id);
```

### 2. Правильно обрабатывайте результаты

```csharp
// ? Хорошо
var result = await repository.GetByIdAsync(id);
if (result.IsSuccess)
{
    var product = result.Value;
    // Обработка продукта
}
else
{
    logger.LogError("Не удалось получить продукт: {Error}", result.ErrorMessage);
}

// ? Избегайте исключений при неудаче
var product = result.Value; // Может быть null при неудаче
```

### 3. Используйте пагинацию для больших наборов данных

```csharp
// ? Хорошо - используйте пагинацию
var pagedResult = await repository.GetPagedAsync(1, 20);

// ? Избегайте - загрузка всех данных
var allResult = await repository.GetAllAsync(); // Может загрузить миллионы записей
```

### 4. Используйте спецификации для сложных запросов

```csharp
// ? Хорошо - переиспользуемая спецификация
var spec = new ActiveProductsWithCategorySpec();
var result = await repository.GetBySpecificationAsync(spec);

// ? Избегайте - жестко закодированные запросы
var result = await repository.GetWhereAsync(p => p.IsActive && p.Category != null);
```

### 5. Используйте транзакции для связанных операций

```csharp
// ? Хорошо - атомарная операция
await repository.TransactionAsync(async repo =>
{
    await repo.AddAsync(order);
    await repo.UpdateAsync(product);
    return Result.Success();
});

// ? Избегайте - отдельные операции без транзакции
await repository.AddAsync(order);
await repository.SaveChangesAsync();
await repository.UpdateAsync(product);
await repository.SaveChangesAsync();
```

### 6. Используйте потоковую передачу для обработки больших данных

```csharp
// ? Хорошо - эффективно по памяти
await foreach (var result in repository.GetAllAsStreamAsync(1000))
{
    // Обработка пакетами
}

// ? Избегайте - загрузка всего в память
var all = await repository.GetAllAsync();
foreach (var item in all.Value)
{
    // Может вызвать проблемы с памятью
}
```

## ?? Руководство по миграции

### От прямого использования EF Core

**До:**
```csharp
var products = await context.Products
    .Where(p => p.IsActive)
    .Include(p => p.Category)
    .ToListAsync();
```

**После:**
```csharp
var result = await repository.GetWhereWithIncludeAsync(
    p => p.IsActive,
    cancellationToken,
    p => p.Category
);

if (result.IsSuccess)
{
    var products = result.Value;
    // Использование продуктов
}
```

### От обработки ошибок на основе исключений

**До:**
```csharp
try
{
    var product = await context.Products.FindAsync(id);
    if (product == null)
        throw new NotFoundException($"Продукт {id} не найден");
    return product;
}
catch (Exception ex)
{
    logger.LogError(ex, "Ошибка получения продукта");
    throw;
}
```

**После:**
```csharp
var result = await repository.GetByIdAsync(id);
return result.Match(
    product => product,
    error => {
        logger.LogError("Ошибка получения продукта: {Error}", error);
        return null; // или обработайте соответствующим образом
    }
);
```