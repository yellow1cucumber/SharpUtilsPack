namespace Results
{
    /// <summary>
    /// Represents the result of an operation, encapsulating either a value or an error message.
    /// </summary>
    /// <typeparam name="T">The type of the value returned on success.</typeparam>
    public class Result<T>
    {
        /// <summary>
        /// Gets the value of the result if successful; otherwise, <c>null</c>.
        /// </summary>
        public T? Value { get; }

        /// <summary>
        /// Gets the error message if the result is a failure; otherwise, <c>null</c>.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets a value indicating whether the result is successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets a value indicating whether the result is a failure.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Initializes a new instance of the <see cref="Result{T}"/> class.
        /// </summary>
        /// <param name="value">The value of the result.</param>
        /// <param name="isSuccess">Indicates if the result is successful.</param>
        /// <param name="errorMessage">The error message if the result is a failure.</param>
        protected Result(T? value, bool isSuccess, string? errorMessage)
        {
            this.Value = value;
            this.IsSuccess = isSuccess;
            this.ErrorMessage = errorMessage;
        }

        #region FABRICS
        /// <summary>
        /// Creates a successful result containing the specified value.
        /// </summary>
        /// <param name="value">The value to encapsulate.</param>
        /// <returns>A successful <see cref="Result{T}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <c>null</c>.</exception>
        public static Result<T> Success(T value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            return new(value, true, null);
        }

        /// <summary>
        /// Creates a failed result containing the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message to encapsulate.</param>
        /// <returns>A failed <see cref="Result{T}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="errorMessage"/> is <c>null</c>.</exception>
        public static Result<T> Failure(string errorMessage)
        {
            if (errorMessage is null)
                throw new ArgumentNullException(nameof(errorMessage));

            return new(default, false, errorMessage);
        }
        #endregion

        #region CALLBACKS
        /// <summary>
        /// Executes the specified action if the result is successful.
        /// </summary>
        /// <param name="action">The action to execute with the value.</param>
        /// <returns>The current <see cref="Result{T}"/> instance.</returns>
        public Result<T> OnSuccess(Action<T> action)
        {
            if (this.IsSuccess && this.Value is not null)
                action(this.Value);
            return this;
        }

        /// <summary>
        /// Executes the specified action if the result is a failure.
        /// </summary>
        /// <param name="action">The action to execute with the error message.</param>
        /// <returns>The current <see cref="Result{T}"/> instance.</returns>
        public Result<T> OnFailure(Action<string> action)
        {
            if (this.IsFailure && this.ErrorMessage is not null)
                action(this.ErrorMessage);
            return this;
        }
        #endregion

        #region UTILS
        /// <summary>
        /// Matches the result and executes the corresponding function based on success or failure.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the match functions.</typeparam>
        /// <param name="onSuccess">Function to execute if successful.</param>
        /// <param name="onFailure">Function to execute if failed.</param>
        /// <returns>The result of the executed function.</returns>
        public TResult Match<TResult>(
            Func<T, TResult> onSuccess,
            Func<string, TResult> onFailure)
        {
            return this.IsSuccess ? onSuccess(this.Value!) : onFailure(this.ErrorMessage!);
        }

        /// <summary>
        /// Maps the value of a successful result to a new type.
        /// </summary>
        /// <typeparam name="U">The type to map to.</typeparam>
        /// <param name="mapper">The mapping function.</param>
        /// <returns>A new <see cref="Result{U}"/> with the mapped value or the original error.</returns>
        public Result<U> Map<U>(Func<T, U> mapper) =>
            this.IsSuccess
                ? Result<U>.Success(mapper(this.Value!))
                : Result<U>.Failure(this.ErrorMessage!);

        /// <summary>
        /// Binds the value of a successful result to a new result.
        /// </summary>
        /// <typeparam name="U">The type of the new result.</typeparam>
        /// <param name="binder">The binding function.</param>
        /// <returns>The result of the binding function or the original error.</returns>
        public Result<U> Bind<U>(Func<T, Result<U>> binder) =>
            this.IsSuccess
                ? binder(this.Value!)
                : Result<U>.Failure(this.ErrorMessage!);

        /// <summary>
        /// Gets the value if successful; otherwise, returns the specified default value.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the result is a failure.</param>
        /// <returns>The value or the default value.</returns>
        public T ValueOrDefault(T defaultValue = default!) =>
            IsSuccess ? Value! : defaultValue;
        #endregion

        #region OPERATORS
        /// <summary>
        /// Implicitly converts a value to a successful result.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator Result<T>(T value) => Success(value);

        /// <summary>
        /// Implicitly converts a result to a boolean indicating success.
        /// </summary>
        /// <param name="result">The result to convert.</param>
        public static implicit operator bool(Result<T> result) => result.IsSuccess;

        /// <summary>
        /// Implicitly converts a successful result to its value.
        /// </summary>
        /// <param name="result">The result to convert.</param>
        /// <exception cref="InvalidOperationException">Thrown if the result is a failure.</exception>
        public static implicit operator T(Result<T> result)
        {
            if (result.IsFailure)
                throw new InvalidOperationException("Cannot extract value from a failed Result.");
            return result.Value!;
        }
        #endregion

        #region OVERRIDE
        /// <summary>
        /// Returns a string representation of the result.
        /// </summary>
        /// <returns>A string describing the result.</returns>
        public override string ToString() =>
            this.IsSuccess ? $"Success({this.Value})" : $"Failure({this.ErrorMessage})";

        /// <summary>
        /// Determines whether the specified object is equal to the current result.
        /// </summary>
        /// <param name="obj">The object to compare with the current result.</param>
        /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj)
        {
            return obj is Result<T> result &&
                   EqualityComparer<T?>.Default.Equals(this.Value, result.Value) &&
                   this.ErrorMessage == result.ErrorMessage &&
                   this.IsSuccess == result.IsSuccess;
        }

        /// <summary>
        /// Returns a hash code for the result.
        /// </summary>
        /// <returns>A hash code for the result.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Value, ErrorMessage, IsSuccess);
        }
        #endregion
    }

    /// <summary>
    /// Represents the result of an operation that does not return a value, encapsulating success or an error message.
    /// </summary>
    public class Result : Result<Unit>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> class.
        /// </summary>
        /// <param name="isSuccess">Indicates if the result is successful.</param>
        /// <param name="errorMessage">The error message if the result is a failure.</param>
        private Result(bool isSuccess, string? errorMessage)
            : base(Unit.Value, isSuccess, errorMessage) { }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <returns>A successful <see cref="Result"/>.</returns>
        public static Result Success() => new(true, null);

        /// <summary>
        /// Creates a failed result containing the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message to encapsulate.</param>
        /// <returns>A failed <see cref="Result"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="errorMessage"/> is <c>null</c>.</exception>
        public static new Result Failure(string errorMessage)
        {
            if (errorMessage is null)
                throw new ArgumentNullException(nameof(errorMessage));

            return new(false, errorMessage);
        }
    }
}
