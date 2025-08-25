namespace Results
{
    public class PaginatedResult<T> : Result<IEnumerable<T>>
    {
        public uint Page { get; init; }
        public uint PageSize { get; init; }
        public uint TotalItems { get; init; }
        public uint TotalPages => (uint)Math.Ceiling((double)TotalItems / PageSize);

        private PaginatedResult(IEnumerable<T>? value, 
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
        public static PaginatedResult<T> Success(IEnumerable<T> value,
                                                 uint page,
                                                 uint pageSize,
                                                 uint totalItems)
            => new(value, true, null, page, pageSize, totalItems);

        public static new PaginatedResult<T> Failure(string errorMessage) =>
            new(default, false, errorMessage, 0, 0, 0);

        public static PaginatedResult<T> Empty(uint page, uint pageSize) =>
            new(Enumerable.Empty<T>(), true, null, page, pageSize, 0);
        #endregion

        #region UTILS

        #region MAP
        public PaginatedResult<U> MapItems<U>(Func<T, U> mapper)
        {
            if (this.IsFailure || this.Value == null)
                return PaginatedResult<U>.Failure(this.ErrorMessage ?? "Unknown error");

            var mapped = this.Value.Select(mapper);

            return !mapped.Any()
                ? PaginatedResult<U>.Empty(this.Page, this.PageSize)
                : PaginatedResult<U>.Success(mapped, this.Page, this.PageSize, this.TotalItems);
        }

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
        public PaginatedResult<U> BindItems<U>(Func<T, Result<U>> binder)
        {
            if (this.IsFailure || this.Value == null)
                return PaginatedResult<U>.Failure(this.ErrorMessage ?? "Unknown error");

            var results = new List<U>();
            foreach (var item in this.Value)
            {
                var r = binder(item);
                if (!r.IsFailure)
                    return PaginatedResult<U>.Failure(r.ErrorMessage ?? "Unknown error");

                results.Add(r.Value!);
            }
            return !results.Any()
                ? PaginatedResult<U>.Empty(this.Page, this.PageSize)
                : PaginatedResult<U>.Success(results, this.Page, this.PageSize, this.TotalItems);
        }

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
