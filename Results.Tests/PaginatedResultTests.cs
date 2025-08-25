using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Results;
using Xunit;

namespace Results.Tests
{
    /// <summary>
    /// Contains unit tests for the <see cref="PaginatedResult{T}"/> class.
    /// </summary>
    public class PaginatedResultTests
    {
        #region Constructor and Properties Tests

        /// <summary>
        /// Verifies that the constructor sets properties correctly for a successful result.
        /// </summary>
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var items = new[] { 1, 2, 3 };
            const uint page = 2;
            const uint pageSize = 10;
            const uint totalItems = 23;

            // Act
            var result = PaginatedResult<int>.Success(items, page, pageSize, totalItems);

            // Assert
            Assert.Equal(page, result.Page);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(totalItems, result.TotalItems);
            Assert.Equal(items, result.Value);
            Assert.True(result.IsSuccess);
        }

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.TotalPages"/> calculates the correct number of pages.
        /// </summary>
        [Fact]
        public void TotalPages_CalculatesCorrectly()
        {
            // Test cases with different combinations of totalItems and pageSize
            var testCases = new[]
            {
                    (totalItems: 10u, pageSize: 3u, expectedPages: 4u),
                    (totalItems: 10u, pageSize: 5u, expectedPages: 2u),
                    (totalItems: 0u, pageSize: 5u, expectedPages: 0u),
                    (totalItems: 11u, pageSize: 5u, expectedPages: 3u),
                    (totalItems: 5u, pageSize: 10u, expectedPages: 1u)
                };

            foreach (var (totalItems, pageSize, expectedPages) in testCases)
            {
                // Arrange
                var result = PaginatedResult<int>.Success(Array.Empty<int>(), 1, pageSize, totalItems);

                // Assert
                Assert.Equal(expectedPages, result.TotalPages);
            }
        }

        #endregion

        #region Factory Methods Tests

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.Success"/> returns a successful result.
        /// </summary>
        [Fact]
        public void Success_ReturnsSuccessfulResult()
        {
            // Arrange
            var items = new[] { 1, 2, 3 };

            // Act
            var result = PaginatedResult<int>.Success(items, 1, 10, 3);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Equal(items, result.Value);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(1u, result.Page);
            Assert.Equal(10u, result.PageSize);
            Assert.Equal(3u, result.TotalItems);
            Assert.Equal(1u, result.TotalPages);
        }

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.Failure"/> returns a failed result.
        /// </summary>
        [Fact]
        public void Failure_ReturnsFailedResult()
        {
            // Arrange
            const string errorMessage = "An error occurred";

            // Act
            var result = PaginatedResult<int>.Failure(errorMessage);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailure);
            Assert.Null(result.Value);
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.Equal(0u, result.Page);
            Assert.Equal(0u, result.PageSize);
            Assert.Equal(0u, result.TotalItems);
            Assert.Equal(0u, result.TotalPages);
        }

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.Empty"/> returns an empty successful result.
        /// </summary>
        [Fact]
        public void Empty_ReturnsEmptySuccessfulResult()
        {
            // Arrange
            const uint page = 2;
            const uint pageSize = 5;

            // Act
            var result = PaginatedResult<int>.Empty(page, pageSize);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value!);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(page, result.Page);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(0u, result.TotalItems);
            Assert.Equal(0u, result.TotalPages);
        }

        #endregion

        #region MapItems Tests

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.MapItems{TResult}"/> maps items for a successful result.
        /// </summary>
        [Fact]
        public void MapItems_WithSuccessResult_MapsItems()
        {
            // Arrange
            var items = new[] { 1, 2, 3 };
            var result = PaginatedResult<int>.Success(items, 2, 5, 10);

            // Act
            var mapped = result.MapItems(i => i.ToString());

            // Assert
            Assert.True(mapped.IsSuccess);
            Assert.Equal(new[] { "1", "2", "3" }, mapped.Value);
            Assert.Equal(result.Page, mapped.Page);
            Assert.Equal(result.PageSize, mapped.PageSize);
            Assert.Equal(result.TotalItems, mapped.TotalItems);
        }

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.MapItems{TResult}"/> returns a mapped failure for a failed result.
        /// </summary>
        [Fact]
        public void MapItems_WithFailureResult_ReturnsMappedFailure()
        {
            // Arrange
            const string errorMessage = "Original error";
            var result = PaginatedResult<int>.Failure(errorMessage);

            // Act
            var mapped = result.MapItems(i => i.ToString());

            // Assert
            Assert.True(mapped.IsFailure);
            Assert.Equal(errorMessage, mapped.ErrorMessage);
            Assert.Null(mapped.Value);
        }

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.MapItems{TResult}"/> returns an empty result for an empty collection.
        /// </summary>
        [Fact]
        public void MapItems_WithEmptyResult_ReturnsEmptyResult()
        {
            // Arrange
            var result = PaginatedResult<int>.Empty(1, 10);

            // Act
            var mapped = result.MapItems(i => i.ToString());

            // Assert
            Assert.True(mapped.IsSuccess);
            Assert.NotNull(mapped.Value);
            Assert.Empty(mapped.Value!);
            Assert.Equal(result.Page, mapped.Page);
            Assert.Equal(result.PageSize, mapped.PageSize);
        }

        #endregion

        #region MapItemsAsync Tests

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.MapItemsAsync{TResult}"/> maps items asynchronously for a successful result.
        /// </summary>
        [Fact]
        public void MapItemsAsync_WithSuccessResult_MapsItemsAsynchronously()
        {
            // Arrange
            var items = new[] { 1, 2, 3 };
            var result = PaginatedResult<int>.Success(items, 2, 5, 10);

            // Act
            var mapped = result.MapItemsAsync(i => Task.FromResult(i * 2));

            // Assert
            Assert.True(mapped.IsSuccess);
            Assert.Equal(new[] { 2, 4, 6 }, mapped.Value);
            Assert.Equal(result.Page, mapped.Page);
            Assert.Equal(result.PageSize, mapped.PageSize);
            Assert.Equal(result.TotalItems, mapped.TotalItems);
        }

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.MapItemsAsync{TResult}"/> returns a mapped failure for a failed result.
        /// </summary>
        [Fact]
        public void MapItemsAsync_WithFailureResult_ReturnsMappedFailure()
        {
            // Arrange
            const string errorMessage = "Original async error";
            var result = PaginatedResult<int>.Failure(errorMessage);

            // Act
            var mapped = result.MapItemsAsync(i => Task.FromResult(i * 2));

            // Assert
            Assert.True(mapped.IsFailure);
            Assert.Equal(errorMessage, mapped.ErrorMessage);
            Assert.Null(mapped.Value);
        }

        #endregion

        #region BindItems Tests

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.BindItems{TResult}"/> binds items for a successful result.
        /// </summary>
        [Fact]
        public void BindItems_WithSuccessResult_BindsItems()
        {
            // Arrange
            var items = new[] { 1, 2, 3 };
            var result = PaginatedResult<int>.Success(items, 2, 5, 10);

            // Act
            var bound = result.BindItems(i => Result<string>.Success(i.ToString()));

            // Assert
            Assert.True(bound.IsSuccess);
            Assert.Equal(new[] { "1", "2", "3" }, bound.Value);
            Assert.Equal(result.Page, bound.Page);
            Assert.Equal(result.PageSize, bound.PageSize);
            Assert.Equal(result.TotalItems, bound.TotalItems);
        }

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.BindItems{TResult}"/> returns a bind failure for a failed result.
        /// </summary>
        [Fact]
        public void BindItems_WithFailureResult_ReturnsBindFailure()
        {
            // Arrange
            const string errorMessage = "Original error";
            var result = PaginatedResult<int>.Failure(errorMessage);

            // Act
            var bound = result.BindItems(i => Result<string>.Success(i.ToString()));

            // Assert
            Assert.True(bound.IsFailure);
            Assert.Equal(errorMessage, bound.ErrorMessage);
            Assert.Null(bound.Value);
        }

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.BindItems{TResult}"/> returns a binder failure if the binder returns a failed result.
        /// </summary>
        [Fact]
        public void BindItems_WithBinderReturningFailure_ReturnsBinderFailure()
        {
            // Arrange
            var items = new[] { 1, 2, 3 };
            var result = PaginatedResult<int>.Success(items, 2, 5, 10);
            const string bindErrorMessage = "Binding failed";

            // Act
            var bound = result.BindItems(i => i == 2
                ? Result<string>.Failure(bindErrorMessage)
                : Result<string>.Success(i.ToString()));

            // Assert
            Assert.True(bound.IsFailure);
            Assert.Equal(bindErrorMessage, bound.ErrorMessage);
            Assert.Null(bound.Value);
        }

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.BindItems{TResult}"/> returns an empty result for an empty collection.
        /// </summary>
        [Fact]
        public void BindItems_WithEmptyCollection_ReturnsEmptyResult()
        {
            // Arrange
            var result = PaginatedResult<int>.Empty(2, 5);

            // Act
            var bound = result.BindItems(i => Result<string>.Success(i.ToString()));

            // Assert
            Assert.True(bound.IsSuccess);
            Assert.NotNull(bound.Value);
            Assert.Empty(bound.Value!);
            Assert.Equal(result.Page, bound.Page);
            Assert.Equal(result.PageSize, bound.PageSize);
        }

        #endregion

        #region BindItemsAsync Tests

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.BindItemsAsync{TResult}"/> binds items asynchronously for a successful result.
        /// </summary>
        [Fact]
        public void BindItemsAsync_WithSuccessResult_BindsItemsAsynchronously()
        {
            // Arrange
            var items = new[] { 1, 2, 3 };
            var result = PaginatedResult<int>.Success(items, 2, 5, 10);

            // Act
            var bound = result.BindItemsAsync(i => Task.FromResult(Result<string>.Success(i.ToString())));

            // Assert
            Assert.True(bound.IsSuccess);
            Assert.Equal(new[] { "1", "2", "3" }, bound.Value);
            Assert.Equal(result.Page, bound.Page);
            Assert.Equal(result.PageSize, bound.PageSize);
            Assert.Equal(result.TotalItems, bound.TotalItems);
        }

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.BindItemsAsync{TResult}"/> returns a bind failure for a failed result.
        /// </summary>
        [Fact]
        public void BindItemsAsync_WithFailureResult_ReturnsBindFailure()
        {
            // Arrange
            const string errorMessage = "Original async error";
            var result = PaginatedResult<int>.Failure(errorMessage);

            // Act
            var bound = result.BindItemsAsync(i => Task.FromResult(Result<string>.Success(i.ToString())));

            // Assert
            Assert.True(bound.IsFailure);
            Assert.Equal(errorMessage, bound.ErrorMessage);
            Assert.Null(bound.Value);
        }

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.BindItemsAsync{TResult}"/> returns a binder failure if the binder returns a failed result asynchronously.
        /// </summary>
        [Fact]
        public void BindItemsAsync_WithBinderReturningFailure_ReturnsBinderFailure()
        {
            // Arrange
            var items = new[] { 1, 2, 3 };
            var result = PaginatedResult<int>.Success(items, 2, 5, 10);
            const string bindErrorMessage = "Async binding failed";

            // Act
            var bound = result.BindItemsAsync(i => Task.FromResult(
                i == 2 ? Result<string>.Failure(bindErrorMessage) : Result<string>.Success(i.ToString())
            ));

            // Assert
            Assert.True(bound.IsFailure);
            Assert.Equal(bindErrorMessage, bound.ErrorMessage);
            Assert.Null(bound.Value);
        }

        /// <summary>
        /// Verifies that <see cref="PaginatedResult{T}.BindItemsAsync{TResult}"/> returns an empty result for an empty collection.
        /// </summary>
        [Fact]
        public void BindItemsAsync_WithEmptyCollection_ReturnsEmptyResult()
        {
            // Arrange
            var result = PaginatedResult<int>.Empty(2, 5);

            // Act
            var bound = result.BindItemsAsync(i => Task.FromResult(Result<string>.Success(i.ToString())));

            // Assert
            Assert.True(bound.IsSuccess);
            Assert.NotNull(bound.Value);
            Assert.Empty(bound.Value!);
            Assert.Equal(result.Page, bound.Page);
            Assert.Equal(result.PageSize, bound.PageSize);
        }

        #endregion
    }
}