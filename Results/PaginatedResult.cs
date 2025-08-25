namespace Results
{
    /// <summary>
    /// Represents a paginated result of an operation, encapsulating a collection of items along with pagination metadata.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public class PaginatedResult<T> : Result<IEnumerable<T>>
    {
        /// <summary>
        /// Gets the current page number (1-based).
        /// </summary>
        public uint Page { get; init; }

        /// <summary>
        /// Gets the number of items per page.
        /// </summary>
        public uint PageSize { get; init; }

        /// <summary>
        /// Gets the total number of items across all pages.
        /// </summary>
        public uint TotalItems { get; init; }

        /// <summary>
        /// Gets the total number of pages, calculated from TotalItems and PageSize.
        /// </summary>
        public uint TotalPages => (uint)Math.Ceiling((double)TotalItems / PageSize);

        /// <summary>
        /// Initializes a new instance of the <see cref="PaginatedResult{T}"/> class.
        /// </summary>
        /// <param name="value">The collection of items for the current page.</param>
        /// <param name="isSuccess">Indicates if the result is successful.</param>
        /// <param name="errorMessage">The error message if the result is a failure.</param>
        /// <param name="page">The current page number.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="totalItems">The total number of items across all pages.</param>
        protected PaginatedResult(IEnumerable<T>? value, 
                                bool isSuccess, 
                                string? errorMessage, 
                                uint page, 
                                uint pageSize, 
                                uint totalItems
        ) : base(value, isSuccess, errorMessage)
        {
            this.Page = page;
            this.PageSize = pageSize;
            this.TotalItems = totalItems;
        }


        #region FABRICS
        /// <summary>
        /// Creates a successful paginated result containing the specified items and pagination metadata.
        /// </summary>
        /// <param name="value">The collection of items for the current page.</param>
        /// <param name="page">The current page number.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="totalItems">The total number of items across all pages.</param>
        /// <returns>A successful <see cref="PaginatedResult{T}"/>.</returns>
        public static PaginatedResult<T> Success(IEnumerable<T> value,
                                                 uint page,
                                                 uint pageSize,
                                                 uint totalItems)
            => new(value, true, null, page, pageSize, totalItems);

        /// <summary>
        /// Creates a failed paginated result containing the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message to encapsulate.</param>
        /// <returns>A failed <see cref="PaginatedResult{T}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="errorMessage"/> is <c>null</c>.</exception>
        public static new PaginatedResult<T> Failure(string errorMessage) =>
            new(default, false, errorMessage, 0, 0, 0);

        /// <summary>
        /// Creates a successful paginated result with an empty collection of items.
        /// </summary>
        /// <param name="page">The current page number.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A successful <see cref="PaginatedResult{T}"/> with an empty collection.</returns>
        public static PaginatedResult<T> Empty(uint page, uint pageSize) =>
            new(Enumerable.Empty<T>(), true, null, page, pageSize, 0);
        #endregion

        #region UTILS

        #region MAP
        /// <summary>
        /// Maps each item in the collection to a new type while preserving pagination metadata.
        /// </summary>
        /// <typeparam name="U">The type to map to.</typeparam>
        /// <param name="mapper">The mapping function applied to each item.</param>
        /// <returns>A new <see cref="PaginatedResult{U}"/> with the mapped items or the original error.</returns>
        public PaginatedResult<U> MapItems<U>(Func<T, U> mapper)
        {
            if (this.IsFailure || this.Value == null)
                return PaginatedResult<U>.Failure(this.ErrorMessage ?? "Unknown error");

            var mapped = this.Value.Select(mapper);

            return !mapped.Any()
                ? PaginatedResult<U>.Empty(this.Page, this.PageSize)
                : PaginatedResult<U>.Success(mapped, this.Page, this.PageSize, this.TotalItems);
        }

        /// <summary>
        /// Maps each item in the collection to a new type asynchronously while preserving pagination metadata.
        /// </summary>
        /// <typeparam name="U">The type to map to.</typeparam>
        /// <param name="mapper">The asynchronous mapping function applied to each item.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>A new <see cref="PaginatedResult{U}"/> with the mapped items or the original error.</returns>
        public PaginatedResult<U> MapItemsAsync<U>(Func<T, Task<U>> mapper, CancellationToken cancellationToken = default)
        {
            if (this.IsFailure || this.Value == null)
                return PaginatedResult<U>.Failure(this.ErrorMessage ?? "Unknown error");

            var tasks = this.Value.Select(item => mapper(item));
            var mapped = Task.WhenAll(tasks).GetAwaiter().GetResult();

            return !mapped.Any()
                ? PaginatedResult<U>.Empty(this.Page, this.PageSize)
                : PaginatedResult<U>.Success(mapped, this.Page, this.PageSize, this.TotalItems);
        }
        #endregion

        #region BIND
        /// <summary>
        /// Binds each item in the collection to a new result while preserving pagination metadata.
        /// </summary>
        /// <typeparam name="U">The type of the new result.</typeparam>
        /// <param name="binder">The binding function applied to each item.</param>
        /// <returns>A new <see cref="PaginatedResult{U}"/> with the bound items or the original error.</returns>
        public PaginatedResult<U> BindItems<U>(Func<T, Result<U>> binder)
        {
            if (this.IsFailure || this.Value == null)
                return PaginatedResult<U>.Failure(this.ErrorMessage ?? "Unknown error");

            var results = new List<U>();
            foreach (var item in this.Value)
            {
                var r = binder(item);
                if (r.IsFailure)
                    return PaginatedResult<U>.Failure(r.ErrorMessage ?? "Unknown error");

                results.Add(r.Value!);
            }
            return !results.Any()
                ? PaginatedResult<U>.Empty(this.Page, this.PageSize)
                : PaginatedResult<U>.Success(results, this.Page, this.PageSize, this.TotalItems);
        }

        /// <summary>
        /// Binds each item in the collection to a new result asynchronously while preserving pagination metadata.
        /// </summary>
        /// <typeparam name="U">The type of the new result.</typeparam>
        /// <param name="binder">The asynchronous binding function applied to each item.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>A new <see cref="PaginatedResult{U}"/> with the bound items or the original error.</returns>
        public PaginatedResult<U> BindItemsAsync<U>(Func<T, Task<Result<U>>> binder, CancellationToken cancellationToken = default)
        {
            if (this.IsFailure || this.Value == null)
                return PaginatedResult<U>.Failure(this.ErrorMessage ?? "Unknown error");

            var results = new List<U>();
            foreach (var item in this.Value)
            {
                var r = binder(item).GetAwaiter().GetResult();
                if (r.IsFailure)
                    return PaginatedResult<U>.Failure(r.ErrorMessage ?? "Unknown error");
                results.Add(r.Value!);
            }
            return !results.Any()
                ? PaginatedResult<U>.Empty(this.Page, this.PageSize)
                : PaginatedResult<U>.Success(results, this.Page, this.PageSize, this.TotalItems);
        }
        #endregion

        #endregion
    }
}
