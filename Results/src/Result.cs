namespace YellowCucumber.Results
{
    /// <summary>
    /// Represents the result of an operation, encapsulating either a value or an error message.
    /// This class provides a functional approach to error handling by avoiding exceptions for expected failures.
    /// </summary>
    /// <typeparam name="T">The type of the value returned on success.</typeparam>
    public class Result<T>
    {
        /// <summary>
        /// Gets the value of the result if successful; otherwise, <c>null</c>.
        /// This property should only be accessed after checking <see cref="IsSuccess"/>.
        /// </summary>
        public T? Value { get; }

        /// <summary>
        /// Gets the error message if the result is a failure; otherwise, <c>null</c>.
        /// This property should only be accessed after checking <see cref="IsFailure"/>.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets a value indicating whether the result is successful.
        /// When true, <see cref="Value"/> contains a valid value and <see cref="ErrorMessage"/> is null.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets a value indicating whether the result is a failure.
        /// When true, <see cref="ErrorMessage"/> contains an error message and <see cref="Value"/> is null.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Initializes a new instance of the <see cref="Result{T}"/> class.
        /// This constructor is protected to ensure that instances are created using the factory methods.
        /// </summary>
        /// <param name="value">The value of the result if successful.</param>
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
        /// <returns>A successful <see cref="Result{T}"/> containing the specified value.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Use this method to create a result that represents a successful operation.
        /// </remarks>
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
        /// <returns>A failed <see cref="Result{T}"/> containing the specified error message.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="errorMessage"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Use this method to create a result that represents a failed operation.
        /// </remarks>
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
        /// <returns>The current <see cref="Result{T}"/> instance to enable method chaining.</returns>
        /// <remarks>
        /// This method provides a convenient way to perform operations on successful results without
        /// affecting the result itself. If the result is a failure, the action is not executed.
        /// </remarks>
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
        /// <returns>The current <see cref="Result{T}"/> instance to enable method chaining.</returns>
        /// <remarks>
        /// This method provides a convenient way to perform operations on failed results without
        /// affecting the result itself. If the result is successful, the action is not executed.
        /// </remarks>
        public Result<T> OnFailure(Action<string> action)
        {
            if (this.IsFailure && this.ErrorMessage is not null)
                action(this.ErrorMessage);
            return this;
        }
        #endregion

        #region UTILS

        #region MATCH
        /// <summary>
        /// Matches the result and executes the corresponding function based on success or failure.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the match functions.</typeparam>
        /// <param name="onSuccess">Function to execute if successful. Receives the value and returns a result.</param>
        /// <param name="onFailure">Function to execute if failed. Receives the error message and returns a result.</param>
        /// <returns>The result of the executed function.</returns>
        /// <remarks>
        /// This method implements a pattern matching approach to handle both success and failure cases
        /// with a uniform interface, similar to the match expressions in functional languages.
        /// </remarks>
        public TResult Match<TResult>(
            Func<T, TResult> onSuccess,
            Func<string, TResult> onFailure)
        {
            return this.IsSuccess ? onSuccess(this.Value!) : onFailure(this.ErrorMessage!);
        }

        /// <summary>
        /// Asynchronously matches the result and executes the corresponding function based on success or failure.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the match functions.</typeparam>
        /// <param name="onSuccess">Asynchronous function to execute if successful. Receives the value and returns a task with a result.</param>
        /// <param name="onFailure">Asynchronous function to execute if failed. Receives the error message and returns a task with a result.</param>
        /// <returns>A task representing the result of the executed function.</returns>
        /// <remarks>
        /// This method provides the same pattern matching functionality as <see cref="Match{TResult}"/>,
        /// but supports asynchronous operations that return tasks.
        /// </remarks>
        public async Task<TResult> MatchAsync<TResult>(
            Func<T, Task<TResult>> onSuccess,
            Func<string, Task<TResult>> onFailure)
        {
            return this.IsSuccess
                ? await onSuccess(this.Value!).ConfigureAwait(false)
                : await onFailure(this.ErrorMessage!).ConfigureAwait(false);
        }
        #endregion

        #region MAP
        /// <summary>
        /// Maps the value of a successful result to a new type using the specified mapping function.
        /// </summary>
        /// <typeparam name="U">The type to map to.</typeparam>
        /// <param name="mapper">The mapping function that transforms the value from type T to type U.</param>
        /// <returns>A new <see cref="Result{U}"/> with the mapped value if successful, or the original error if failed.</returns>
        /// <remarks>
        /// This operation is similar to the 'Select' method in LINQ or 'map' in functional programming.
        /// It applies a transformation to the value inside a successful result without unwrapping it.
        /// </remarks>
        public Result<U> Map<U>(Func<T, U> mapper) =>
            this.IsSuccess
                ? Result<U>.Success(mapper(this.Value!))
                : Result<U>.Failure(this.ErrorMessage!);

        /// <summary>
        /// Asynchronously maps the value of a successful result to a new type using the specified mapping function.
        /// </summary>
        /// <typeparam name="U">The type to map to.</typeparam>
        /// <param name="mapper">The asynchronous mapping function that transforms the value from type T to type U.</param>
        /// <returns>A task representing a new <see cref="Result{U}"/> with the mapped value if successful, or the original error if failed.</returns>
        /// <remarks>
        /// This operation is the asynchronous version of <see cref="Map{U}"/>, supporting mapping functions
        /// that perform asynchronous operations and return tasks.
        /// </remarks>
        public async Task<Result<U>> MapAsync<U>(Func<T, Task<U>> mapper) =>
            this.IsSuccess
                ? Result<U>.Success(await mapper(this.Value!).ConfigureAwait(false))
                : Result<U>.Failure(this.ErrorMessage!);
        #endregion

        #region BIND
        /// <summary>
        /// Binds the value of a successful result to a new result using the specified binding function.
        /// </summary>
        /// <typeparam name="U">The type of the value in the new result.</typeparam>
        /// <param name="binder">The binding function that transforms the value to a new result.</param>
        /// <returns>The result of the binding function if successful, or the original error if failed.</returns>
        /// <remarks>
        /// This operation is similar to 'SelectMany' in LINQ or 'flatMap' in functional programming.
        /// It allows composition of operations that return results, avoiding nested results.
        /// </remarks>
        public Result<U> Bind<U>(Func<T, Result<U>> binder) =>
            this.IsSuccess
                ? binder(this.Value!)
                : Result<U>.Failure(this.ErrorMessage!);

        /// <summary>
        /// Asynchronously binds the value of a successful result to a new result using the specified binding function.
        /// </summary>
        /// <typeparam name="U">The type of the value in the new result.</typeparam>
        /// <param name="binder">The asynchronous binding function that transforms the value to a task with a new result.</param>
        /// <returns>A task representing the result of the binding function if successful, or the original error if failed.</returns>
        /// <remarks>
        /// This operation is the asynchronous version of <see cref="Bind{U}"/>, supporting binding functions
        /// that perform asynchronous operations and return tasks with results.
        /// </remarks>
        public async Task<Result<U>> BindAsync<U>(Func<T, Task<Result<U>>> binder) =>
            this.IsSuccess
                ? await binder(this.Value!).ConfigureAwait(false)
                : Result<U>.Failure(this.ErrorMessage!);
        #endregion

        /// <summary>
        /// Gets the value if successful; otherwise, returns the specified default value.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the result is a failure.</param>
        /// <returns>The value if successful, or the default value if failed.</returns>
        /// <remarks>
        /// This method provides a safe way to extract the value from a result without having to
        /// check <see cref="IsSuccess"/> explicitly.
        /// </remarks>
        public T ValueOrDefault(T defaultValue = default!) =>
            IsSuccess ? Value! : defaultValue;
        #endregion

        #region OPERATORS
        /// <summary>
        /// Implicitly converts a value to a successful result.
        /// </summary>
        /// <param name="value">The value to convert to a successful result.</param>
        /// <remarks>
        /// This operator enables seamless conversion from values to results in contexts
        /// that expect a <see cref="Result{T}"/>.
        /// </remarks>
        public static implicit operator Result<T>(T value) => Success(value);

        /// <summary>
        /// Implicitly converts a result to a boolean indicating success.
        /// </summary>
        /// <param name="result">The result to convert to a boolean.</param>
        /// <remarks>
        /// This operator enables direct use of a <see cref="Result{T}"/> in boolean contexts,
        /// such as if conditions, without having to access <see cref="IsSuccess"/> explicitly.
        /// </remarks>
        public static implicit operator bool(Result<T> result) => result.IsSuccess;

        /// <summary>
        /// Implicitly converts a successful result to its value.
        /// </summary>
        /// <param name="result">The successful result to convert to its value.</param>
        /// <exception cref="InvalidOperationException">Thrown if the result is a failure.</exception>
        /// <remarks>
        /// This operator enables direct use of a <see cref="Result{T}"/> in contexts that expect a value of type T.
        /// Use with caution, as it will throw an exception if the result is a failure.
        /// </remarks>
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
        /// <returns>A string describing the result, including its status and value or error message.</returns>
        /// <remarks>
        /// This method provides a human-readable representation of the result for debugging
        /// and logging purposes.
        /// </remarks>
        public override string ToString() =>
            this.IsSuccess ? $"Success({this.Value})" : $"Failure({this.ErrorMessage})";

        /// <summary>
        /// Determines whether the specified object is equal to the current result.
        /// </summary>
        /// <param name="obj">The object to compare with the current result.</param>
        /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Two results are considered equal if they have the same status (success or failure)
        /// and the same value or error message.
        /// </remarks>
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
        /// <returns>A hash code for the result, combining the hash codes of its value, error message, and status.</returns>
        /// <remarks>
        /// This method ensures that the hash code is consistent with the equality definition
        /// provided by <see cref="Equals(object)"/>.
        /// </remarks>
        public override int GetHashCode() => HashCode.Combine(Value, ErrorMessage, IsSuccess);
        #endregion
    }

    /// <summary>
    /// Represents the result of an operation that does not return a value, encapsulating success or an error message.
    /// This class is a specialized version of <see cref="Result{T}"/> for operations that only need to indicate
    /// success or failure without returning a value.
    /// </summary>
    public class Result : Result<Unit>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> class.
        /// This constructor is private to ensure that instances are created using the factory methods.
        /// </summary>
        /// <param name="isSuccess">Indicates if the result is successful.</param>
        /// <param name="errorMessage">The error message if the result is a failure.</param>
        private Result(bool isSuccess, string? errorMessage)
            : base(Unit.Value, isSuccess, errorMessage) { }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <returns>A successful <see cref="Result"/> containing no value.</returns>
        /// <remarks>
        /// Use this method to create a result that represents a successful operation
        /// that does not produce a meaningful value.
        /// </remarks>
        public static Result Success() => new(true, null);

        /// <summary>
        /// Creates a failed result containing the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message to encapsulate.</param>
        /// <returns>A failed <see cref="Result"/> containing the specified error message.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="errorMessage"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Use this method to create a result that represents a failed operation
        /// that does not produce a value.
        /// </remarks>
        public static new Result Failure(string errorMessage)
        {
            if (errorMessage is null)
                throw new ArgumentNullException(nameof(errorMessage));

            return new(false, errorMessage);
        }
    }
}
