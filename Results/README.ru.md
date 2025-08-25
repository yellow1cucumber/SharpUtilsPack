# Results

Всеобъемлющая функциональная библиотека результатов для .NET 9 и C# 13, предоставляющая надежные паттерны обработки ошибок и примитивы композиции. Эта библиотека помогает устранить обработку ошибок на основе исключений в пользу более функционального подхода, который является типобезопасным, компонуемым и более понятным.

*[English version](README.md)*

[![NuGet](https://img.shields.io/nuget/v/SharpUtils.Results.svg)](https://www.nuget.org/packages/SharpUtils.Results/)
[![Build Status](https://github.com/yellow1cucumber/UtilsPack/workflows/Build%20and%20Test/badge.svg)](https://github.com/yellow1cucumber/UtilsPack/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Обзор

Results — это легковесная библиотека, реализующая паттерн Result (также известный как монада Either в функциональном программировании). Она инкапсулирует результат операции, которая может либо успешно завершиться со значением, либо завершиться с сообщением об ошибке, делая обработку ошибок более явной и предсказуемой.

## Установка

Установите пакет из NuGet:

```
dotnet add package SharpUtils.Results
```

Или через менеджер пакетов NuGet:

```
Install-Package SharpUtils.Results
```

## Основные компоненты

### `Result<T>`

Основной обобщенный класс, представляющий результат операции:

- **Свойства**:
  - `Value`: Значение результата (при успешном выполнении)
  - `ErrorMessage`: Детали ошибки (при сбое)
  - `IsSuccess`: Логическое значение, указывающее на успех
  - `IsFailure`: Логическое значение, указывающее на неудачу

- **Фабричные методы**:
  - `Success(T value)`: Создает успешный результат со значением
  - `Failure(string errorMessage)`: Создает неудачный результат с сообщением об ошибке

- **Сопоставление с образцом**:
  - `Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)`: Обрабатывает случаи успеха и неудачи
  - `MatchAsync<TResult>(...)`: Асинхронная версия Match

- **Трансформации**:
  - `Map<U>(Func<T, U> mapper)`: Преобразует значение, если результат успешен
  - `MapAsync<U>(Func<T, Task<U>> mapper)`: Асинхронно преобразует значение
  - `Bind<U>(Func<T, Result<U>> binder)`: Связывает результаты вместе
  - `BindAsync<U>(Func<T, Task<Result<U>>> binder)`: Асинхронно связывает результаты

- **Обратные вызовы**:
  - `OnSuccess(Action<T> action)`: Выполняет действие, если результат успешен
  - `OnFailure(Action<string> action)`: Выполняет действие, если результат неудачен

- **Извлечение значения**:
  - `ValueOrDefault(T defaultValue)`: Безопасно извлекает значение или возвращает значение по умолчанию

- **Перегрузка операторов**:
  - Неявное преобразование из `T` в `Result<T>`
  - Неявное преобразование из `Result<T>` в `bool` (true, если успешно)
  - Неявное преобразование из `Result<T>` в `T` (выбрасывает исключение, если неудача)

### `Result` (Не-обобщенный)

Специализация для операций, которые не возвращают значение:

- `Success()`: Создает успешный результат без значения
- `Failure(string errorMessage)`: Создает неудачный результат с сообщением об ошибке

### `PaginatedResult<T>`

Расширяет `Result<IEnumerable<T>>` метаданными пагинации:

- **Свойства**:
  - `Page`: Текущий номер страницы
  - `PageSize`: Количество элементов на странице
  - `TotalItems`: Общее количество элементов на всех страницах
  - `TotalPages`: Расчетное общее количество страниц

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

Представляет пустое значение, используемое в не-обобщенной реализации `Result`.

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

// Трансформация результатов
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

// Трансформация всех элементов в коллекции
var upperCaseItems = pagedData.MapItems(item => item.ToUpper());

// Привязка элементов к другим результатам
var boundItems = pagedData.BindItems(item => 
    item.Length > 5 
        ? Result<int>.Success(item.Length) 
        : Result<int>.Failure("Элемент слишком короткий")
);
```

### Асинхронные операции

```csharp
// Асинхронные трансформации
var asyncResult = await Result<int>.Success(42)
    .MapAsync(async x => {
        await Task.Delay(100);
        return x * 2;
    });

// Асинхронная привязка
var asyncBound = await Result<string>.Success("42")
    .BindAsync(async s => {
        await Task.Delay(100);
        if (int.TryParse(s, out var n))
            return Result<int>.Success(n);
        return Result<int>.Failure("Не число");
    });
```

## Паттерны обработки ошибок

### Программирование, ориентированное на железную дорогу (Railway-Oriented Programming)

Эта библиотека поддерживает паттерн программирования, ориентированный на железную дорогу, где функции могут быть скомпонованы вместе, изящно обрабатывая ошибки:

```csharp
// Определяем операции, которые возвращают Results
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

## Целевые фреймворки

- **.NET 9.0**
- **.NET 8.0**
- **.NET 7.0**
- **.NET 6.0**
- **.NET Standard 2.1**
- **.NET Standard 2.0**

## Вклад в проект

Мы приветствуем ваш вклад в проект! Не стесняйтесь отправлять Pull Request.

1. Форкните репозиторий
2. Создайте ветку для вашей функции (`git checkout -b feature/amazing-feature`)
3. Зафиксируйте ваши изменения (`git commit -m 'Добавлена удивительная функция'`)
4. Отправьте изменения в ветку (`git push origin feature/amazing-feature`)
5. Откройте Pull Request

## Лицензия

Этот проект лицензирован под лицензией MIT — см. файл [LICENSE](LICENSE) для подробностей