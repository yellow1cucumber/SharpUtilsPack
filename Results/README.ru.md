# Results

Всеобъемлющая функциональная библиотека результатов для .NET 9 и C# 13, предоставляющая надежные шаблоны обработки ошибок и примитивы композиции. Эта библиотека помогает устранить обработку ошибок на основе исключений в пользу более функционального подхода, который является типобезопасным, компонуемым и более понятным.

*[English version](README.md)*

## Обзор

Results — это легковесная библиотека, реализующая паттерн Result (также известный как монада Either в функциональном программировании). Она инкапсулирует результат операции, которая может либо успешно завершиться со значением, либо завершиться с сообщением об ошибке, делая обработку ошибок более явной и предсказуемой.

## Ключевые компоненты

### `Result<T>`

Основной обобщенный класс, представляющий результат операции:

- **Свойства**:
  - `Value`: Значение результата (при успешном завершении)
  - `ErrorMessage`: Детали ошибки (при неудаче)
  - `IsSuccess`: Логическое значение, указывающее на успех
  - `IsFailure`: Логическое значение, указывающее на неудачу

- **Фабричные методы**:
  - `Success(T value)`: Создает успешный результат с значением
  - `Failure(string errorMessage)`: Создает неудачный результат с сообщением об ошибке

- **Сопоставление с образцом**:
  - `Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)`: Обрабатывает случаи успеха и неудачи
  - `MatchAsync<TResult>(...)`: Асинхронная версия Match

- **Трансформация**:
  - `Map<U>(Func<T, U> mapper)`: Преобразует значение, если результат успешен
  - `MapAsync<U>(Func<T, Task<U>> mapper)`: Асинхронно преобразует значение
  - `Bind<U>(Func<T, Result<U>> binder)`: Связывает результаты вместе
  - `BindAsync<U>(Func<T, Task<Result<U>>> binder)`: Асинхронно связывает результаты

- **Обратные вызовы**:
  - `OnSuccess(Action<T> action)`: Выполняет действие, если результат успешный
  - `OnFailure(Action<string> action)`: Выполняет действие, если результат неудачный

- **Извлечение значения**:
  - `ValueOrDefault(T defaultValue)`: Безопасно извлекает значение или возвращает значение по умолчанию

- **Перегрузки операторов**:
  - Неявное преобразование из `T` в `Result<T>`
  - Неявное преобразование из `Result<T>` в `bool` (true, если успешно)
  - Неявное преобразование из `Result<T>` в `T` (вызывает исключение, если результат неудачный)

### `Result` (Без типа)

Специализация для операций, которые не возвращают значение:

- `Success()`: Создает успешный результат без значения
- `Failure(string errorMessage)`: Создает неудачный результат с сообщением об ошибке

### `PaginatedResult<T>`

Расширяет `Result<IEnumerable<T>>` с метаданными пагинации:

- **Свойства**:
  - `Page`: Текущий номер страницы
  - `PageSize`: Количество элементов на странице
  - `TotalItems`: Общее количество элементов на всех страницах
  - `TotalPages`: Рассчитанное общее количество страниц

- **Фабричные методы**:
  - `Success(IEnumerable<T> value, uint page, uint pageSize, uint totalItems)`
  - `Failure(string errorMessage)`
  - `Empty(uint page, uint pageSize)`

- **Специализированные методы**:
  - `MapItems<U>(Func<T, U> mapper)`: Отображает каждый элемент в коллекции
  - `MapItemsAsync<U>(...)`: Асинхронно отображает элементы
  - `BindItems<U>(...)`: Связывает каждый элемент с новым результатом
  - `BindItemsAsync<U>(...)`: Асинхронно связывает элементы

### `Unit`

Представляет пустое значение, используемое в реализации нетипизированного `Result`.

## Примеры использования

### Базовое использование Result

```csharp
using Results;

// Создание успешных и неудачных результатов
var success = Result<int>.Success(42);
var failure = Result<int>.Failure("Что-то пошло не так");

// Проверка статуса результата
if (success.IsSuccess)
{
    Console.WriteLine($"Значение: {success.Value}");
}

// Использование сопоставления с образцом
var message = success.Match(
    value => $"Ответ: {value}",
    error => $"Произошла ошибка: {error}"
);

// Использование обратных вызовов
success
    .OnSuccess(value => Console.WriteLine($"Успех: {value}"))
    .OnFailure(error => Console.WriteLine($"Ошибка: {error}"));

// Преобразование результатов
var transformed = success
    .Map(x => x * 2)
    .Map(x => x.ToString());

// Цепочка операций
var chained = success
    .Bind(x => TryDivide(100, x))
    .Bind(x => TryParse(x.ToString()));

// Использование перегрузок операторов
Result<int> implicitSuccess = 42; // Неявное преобразование из значения
bool isSuccessful = success;      // Неявное преобразование в bool
```

### Использование PaginatedResult

```csharp
// Создание результата с пагинацией
var pagedData = PaginatedResult<string>.Success(
    new[] { "Элемент1", "Элемент2", "Элемент3" },
    page: 1,
    pageSize: 10,
    totalItems: 25
);

// Доступ к информации о пагинации
Console.WriteLine($"Страница {pagedData.Page} из {pagedData.TotalPages}");
Console.WriteLine($"Элементы: {pagedData.Value!.Count()} из {pagedData.TotalItems}");

// Преобразование всех элементов в коллекции
var upperCaseItems = pagedData.MapItems(item => item.ToUpper());

// Связывание элементов с другими результатами
var boundItems = pagedData.BindItems(item => 
    item.Length > 5 
        ? Result<int>.Success(item.Length) 
        : Result<int>.Failure("Элемент слишком короткий")
);
```

### Асинхронные операции

```csharp
// Асинхронные преобразования
var asyncResult = await Result<int>.Success(42)
    .MapAsync(async x => {
        await Task.Delay(100);
        return x * 2;
    });

// Асинхронное связывание
var asyncBound = await Result<string>.Success("42")
    .BindAsync(async s => {
        await Task.Delay(100);
        if (int.TryParse(s, out var n))
            return Result<int>.Success(n);
        return Result<int>.Failure("Не число");
    });
```

## Шаблоны обработки ошибок

### Программирование, ориентированное на железную дорогу (Railway-Oriented Programming)

Эта библиотека поддерживает паттерн программирования, ориентированный на железную дорогу, где функции могут быть скомпонованы вместе, обрабатывая ошибки изящно:

```csharp
// Определение операций, возвращающих Result
Result<User> GetUser(string id) => /* ... */;
Result<Order> GetLatestOrder(User user) => /* ... */;
Result<Receipt> GenerateReceipt(Order order) => /* ... */;
Result<Unit> SendEmail(Receipt receipt, User user) => /* ... */;

// Цепочка операций с автоматической обработкой ошибок
var result = GetUser("user123")
    .Bind(GetLatestOrder)
    .Bind(GenerateReceipt)
    .Bind(receipt => SendEmail(receipt, user));

// Обработка финального результата
result.Match(
    _ => Console.WriteLine("Письмо успешно отправлено"),
    error => Console.WriteLine($"Ошибка: {error}")
);
```

## Целевая платформа

- **.NET 9.0**
- **C# 13.0**

## Установка

Добавьте ссылку на проект в свое решение:

```xml
<ProjectReference Include="Results\Results.csproj" />
```

## Тестирование

Всесторонние модульные тесты предоставляются в проекте `Results.Tests` с использованием xUnit.

## Лицензия

Лицензия MIT (или укажите предпочитаемую лицензию)