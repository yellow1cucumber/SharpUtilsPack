using System;
using System.Threading.Tasks;

using Xunit;

namespace Results.Tests
{
    /// <summary>
    /// Contains unit tests for the <see cref="Result{T}"/> and <see cref="Result"/> classes.
    /// These tests verify the correct behavior of result creation, callbacks, matching, mapping, binding,
    /// value extraction, operator overloads, overrides, and non-generic result scenarios.
    /// </summary>
    public class ResultTest
    {
        #region Creation Tests
        /// <summary>
        /// Verifies that <see cref="Result{T}.Success(T)"/> creates a successful result with the specified value.
        /// </summary>
        [Fact]
        public void Success_WithValidValue_CreatesSuccessResult()
        {
            // Arrange & Act
            var result = Result<int>.Success(42);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Equal(42, result.Value);
            Assert.Null(result.ErrorMessage);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.Success(T)"/> throws <see cref="ArgumentNullException"/> when passed a null value.
        /// </summary>
        [Fact]
        public void Success_WithNullValue_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.Failure(string)"/> creates a failed result with the specified error message.
        /// </summary>
        [Fact]
        public void Failure_WithErrorMessage_CreatesFailureResult()
        {
            // Arrange & Act
            var result = Result<int>.Failure("Something went wrong");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailure);
            Assert.Equal("Something went wrong", result.ErrorMessage);
            Assert.Equal(default, result.Value);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.Failure(string)"/> throws <see cref="ArgumentNullException"/> when passed a null error message.
        /// </summary>
        [Fact]
        public void Failure_WithNullErrorMessage_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => Result<int>.Failure(null!));
        }
        #endregion

        #region Callback Tests
        /// <summary>
        /// Verifies that <see cref="Result{T}.OnSuccess(Action{T})"/> executes the action when the result is successful.
        /// </summary>
        [Fact]
        public void OnSuccess_WhenResultIsSuccess_ExecutesAction()
        {
            // Arrange
            var result = Result<int>.Success(42);
            var wasActionCalled = false;

            // Act
            result.OnSuccess(_ => wasActionCalled = true);

            // Assert
            Assert.True(wasActionCalled);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.OnSuccess(Action{T})"/> does not execute the action when the result is a failure.
        /// </summary>
        [Fact]
        public void OnSuccess_WhenResultIsFailure_DoesNotExecuteAction()
        {
            // Arrange
            var result = Result<int>.Failure("Error");
            var wasActionCalled = false;

            // Act
            result.OnSuccess(_ => wasActionCalled = true);

            // Assert
            Assert.False(wasActionCalled);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.OnFailure(Action{string})"/> executes the action when the result is a failure.
        /// </summary>
        [Fact]
        public void OnFailure_WhenResultIsFailure_ExecutesAction()
        {
            // Arrange
            var result = Result<int>.Failure("Error");
            var wasActionCalled = false;

            // Act
            result.OnFailure(_ => wasActionCalled = true);

            // Assert
            Assert.True(wasActionCalled);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.OnFailure(Action{string})"/> does not execute the action when the result is successful.
        /// </summary>
        [Fact]
        public void OnFailure_WhenResultIsSuccess_DoesNotExecuteAction()
        {
            // Arrange
            var result = Result<int>.Success(42);
            var wasActionCalled = false;

            // Act
            result.OnFailure(_ => wasActionCalled = true);

            // Assert
            Assert.False(wasActionCalled);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.OnSuccess(Action{T})"/> and <see cref="Result{T}.OnFailure(Action{string})"/> can be chained.
        /// </summary>
        [Fact]
        public void OnSuccess_AndOnFailure_CanBeChained()
        {
            // Arrange
            var result = Result<int>.Success(42);
            var successCalled = false;
            var failureCalled = false;

            // Act
            result.OnSuccess(_ => successCalled = true)
                  .OnFailure(_ => failureCalled = true);

            // Assert
            Assert.True(successCalled);
            Assert.False(failureCalled);
        }
        #endregion

        #region Match Tests
        /// <summary>
        /// Verifies that <see cref="Result{T}.Match{TResult}(Func{T,TResult},Func{string,TResult})"/> calls the success function when the result is successful.
        /// </summary>
        [Fact]
        public void Match_WhenResultIsSuccess_CallsSuccessFunc()
        {
            // Arrange
            var result = Result<int>.Success(42);

            // Act
            var output = result.Match(
                v => $"Success: {v}",
                e => $"Failure: {e}"
            );

            // Assert
            Assert.Equal("Success: 42", output);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.Match{TResult}(Func{T,TResult},Func{string,TResult})"/> calls the failure function when the result is a failure.
        /// </summary>
        [Fact]
        public void Match_WhenResultIsFailure_CallsFailureFunc()
        {
            // Arrange
            var result = Result<int>.Failure("Something went wrong");

            // Act
            var output = result.Match(
                v => $"Success: {v}",
                e => $"Failure: {e}"
            );

            // Assert
            Assert.Equal("Failure: Something went wrong", output);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.MatchAsync{TResult}(Func{T,Task{TResult}},Func{string,Task{TResult}})"/> calls the success function asynchronously when the result is successful.
        /// </summary>
        [Fact]
        public async Task MatchAsync_WhenResultIsSuccess_CallsSuccessFunc()
        {
            // Arrange
            var result = Result<int>.Success(42);

            // Act
            var output = await result.MatchAsync(
                v => Task.FromResult($"Success: {v}"),
                e => Task.FromResult($"Failure: {e}")
            );

            // Assert
            Assert.Equal("Success: 42", output);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.MatchAsync{TResult}(Func{T,Task{TResult}},Func{string,Task{TResult}})"/> calls the failure function asynchronously when the result is a failure.
        /// </summary>
        [Fact]
        public async Task MatchAsync_WhenResultIsFailure_CallsFailureFunc()
        {
            // Arrange
            var result = Result<int>.Failure("Something went wrong");

            // Act
            var output = await result.MatchAsync(
                v => Task.FromResult($"Success: {v}"),
                e => Task.FromResult($"Failure: {e}")
            );

            // Assert
            Assert.Equal("Failure: Something went wrong", output);
        }
        #endregion

        #region Map Tests
        /// <summary>
        /// Verifies that <see cref="Result{T}.Map{U}(Func{T,U})"/> transforms the value when the result is successful.
        /// </summary>
        [Fact]
        public void Map_WhenResultIsSuccess_TransformsValue()
        {
            // Arrange
            var result = Result<int>.Success(42);

            // Act
            var mappedResult = result.Map(v => v.ToString());

            // Assert
            Assert.True(mappedResult.IsSuccess);
            Assert.Equal("42", mappedResult.Value);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.Map{U}(Func{T,U})"/> preserves the failure when the result is a failure.
        /// </summary>
        [Fact]
        public void Map_WhenResultIsFailure_PreservesFailure()
        {
            // Arrange
            var result = Result<int>.Failure("Error");

            // Act
            var mappedResult = result.Map(v => v.ToString());

            // Assert
            Assert.True(mappedResult.IsFailure);
            Assert.Equal("Error", mappedResult.ErrorMessage);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.MapAsync{U}(Func{T,Task{U}})"/> transforms the value asynchronously when the result is successful.
        /// </summary>
        [Fact]
        public async Task MapAsync_WhenResultIsSuccess_TransformsValue()
        {
            // Arrange
            var result = Result<int>.Success(42);

            // Act
            var mappedResult = await result.MapAsync(v => Task.FromResult(v.ToString()));

            // Assert
            Assert.True(mappedResult.IsSuccess);
            Assert.Equal("42", mappedResult.Value);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.MapAsync{U}(Func{T,Task{U}})"/> preserves the failure asynchronously when the result is a failure.
        /// </summary>
        [Fact]
        public async Task MapAsync_WhenResultIsFailure_PreservesFailure()
        {
            // Arrange
            var result = Result<int>.Failure("Error");

            // Act
            var mappedResult = await result.MapAsync(v => Task.FromResult(v.ToString()));

            // Assert
            Assert.True(mappedResult.IsFailure);
            Assert.Equal("Error", mappedResult.ErrorMessage);
        }
        #endregion

        #region Bind Tests
        /// <summary>
        /// Verifies that <see cref="Result{T}.Bind{U}(Func{T,Result{U}})"/> applies the binding function when the result is successful.
        /// </summary>
        [Fact]
        public void Bind_WhenResultIsSuccess_AppliesBinding()
        {
            // Arrange
            var result = Result<int>.Success(42);

            // Act
            var boundResult = result.Bind(v => Result<string>.Success(v.ToString()));

            // Assert
            Assert.True(boundResult.IsSuccess);
            Assert.Equal("42", boundResult.Value);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.Bind{U}(Func{T,Result{U}})"/> can return a failure from the binding function.
        /// </summary>
        [Fact]
        public void Bind_WhenResultIsSuccess_CanReturnFailure()
        {
            // Arrange
            var result = Result<int>.Success(42);

            // Act
            var boundResult = result.Bind(_ => Result<string>.Failure("New error"));

            // Assert
            Assert.True(boundResult.IsFailure);
            Assert.Equal("New error", boundResult.ErrorMessage);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.Bind{U}(Func{T,Result{U}})"/> preserves the failure when the result is a failure.
        /// </summary>
        [Fact]
        public void Bind_WhenResultIsFailure_PreservesFailure()
        {
            // Arrange
            var result = Result<int>.Failure("Original error");

            // Act
            var boundResult = result.Bind(v => Result<string>.Success(v.ToString()));

            // Assert
            Assert.True(boundResult.IsFailure);
            Assert.Equal("Original error", boundResult.ErrorMessage);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.BindAsync{U}(Func{T,Task{Result{U}}})"/> applies the binding function asynchronously when the result is successful.
        /// </summary>
        [Fact]
        public async Task BindAsync_WhenResultIsSuccess_AppliesBinding()
        {
            // Arrange
            var result = Result<int>.Success(42);

            // Act
            var boundResult = await result.BindAsync(v =>
                Task.FromResult(Result<string>.Success(v.ToString())));

            // Assert
            Assert.True(boundResult.IsSuccess);
            Assert.Equal("42", boundResult.Value);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.BindAsync{U}(Func{T,Task{Result{U}}})"/> preserves the failure asynchronously when the result is a failure.
        /// </summary>
        [Fact]
        public async Task BindAsync_WhenResultIsFailure_PreservesFailure()
        {
            // Arrange
            var result = Result<int>.Failure("Original error");

            // Act
            var boundResult = await result.BindAsync(v =>
                Task.FromResult(Result<string>.Success(v.ToString())));

            // Assert
            Assert.True(boundResult.IsFailure);
            Assert.Equal("Original error", boundResult.ErrorMessage);
        }
        #endregion

        #region ValueOrDefault Tests
        /// <summary>
        /// Verifies that <see cref="Result{T}.ValueOrDefault(T)"/> returns the value when the result is successful.
        /// </summary>
        [Fact]
        public void ValueOrDefault_WhenResultIsSuccess_ReturnsValue()
        {
            // Arrange
            var result = Result<int>.Success(42);

            // Act
            var value = result.ValueOrDefault();

            // Assert
            Assert.Equal(42, value);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.ValueOrDefault(T)"/> returns the default value when the result is a failure.
        /// </summary>
        [Fact]
        public void ValueOrDefault_WhenResultIsFailure_ReturnsDefaultValue()
        {
            // Arrange
            var result = Result<int>.Failure("Error");

            // Act
            var value = result.ValueOrDefault();

            // Assert
            Assert.Equal(0, value);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.ValueOrDefault(T)"/> returns the specified default value when the result is a failure.
        /// </summary>
        [Fact]
        public void ValueOrDefault_WhenResultIsFailure_ReturnsSpecifiedDefaultValue()
        {
            // Arrange
            var result = Result<int>.Failure("Error");

            // Act
            var value = result.ValueOrDefault(99);

            // Assert
            Assert.Equal(99, value);
        }
        #endregion

        #region Operator Tests
        /// <summary>
        /// Verifies that the implicit operator from value to <see cref="Result{T}"/> creates a successful result.
        /// </summary>
        [Fact]
        public void ImplicitOperator_FromValue_CreatesSuccessResult()
        {
            // Arrange
            Result<int> result = 42;

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        /// <summary>
        /// Verifies that the implicit operator to <see cref="bool"/> returns <see cref="Result{T}.IsSuccess"/>.
        /// </summary>
        [Fact]
        public void ImplicitOperator_ToBoolean_ReturnsIsSuccess()
        {
            // Arrange
            var successResult = Result<int>.Success(42);
            var failureResult = Result<int>.Failure("Error");

            // Act & Assert
            if (successResult)
            {
                Assert.True(true); // Should reach here
            }
            else
            {
                Assert.True(false, "Success result evaluated to false");
            }

            if (failureResult)
            {
                Assert.True(false, "Failure result evaluated to true");
            }
            else
            {
                Assert.True(true); // Should reach here
            }
        }

        /// <summary>
        /// Verifies that the implicit operator to value extracts the value from a successful result.
        /// </summary>
        [Fact]
        public void ImplicitOperator_ToValue_ExtractsValueFromSuccess()
        {
            // Arrange
            var result = Result<int>.Success(42);

            // Act
            int value = result;

            // Assert
            Assert.Equal(42, value);
        }

        /// <summary>
        /// Verifies that the implicit operator to value throws <see cref="InvalidOperationException"/> for a failure result.
        /// </summary>
        [Fact]
        public void ImplicitOperator_ToValue_ThrowsForFailure()
        {
            // Arrange
            var result = Result<int>.Failure("Error");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                int value = result;
            });
        }
        #endregion

        #region Override Tests
        /// <summary>
        /// Verifies that <see cref="Result{T}.ToString"/> returns a formatted string for a successful result.
        /// </summary>
        [Fact]
        public void ToString_ForSuccessResult_ReturnsFormattedString()
        {
            // Arrange
            var result = Result<int>.Success(42);

            // Act
            var str = result.ToString();

            // Assert
            Assert.Equal("Success(42)", str);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.ToString"/> returns a formatted string for a failure result.
        /// </summary>
        [Fact]
        public void ToString_ForFailureResult_ReturnsFormattedString()
        {
            // Arrange
            var result = Result<int>.Failure("Error");

            // Act
            var str = result.ToString();

            // Assert
            Assert.Equal("Failure(Error)", str);
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.Equals(object)"/> returns true for two successful results with the same value.
        /// </summary>
        [Fact]
        public void Equals_WithSameSuccessResult_ReturnsTrue()
        {
            // Arrange
            var result1 = Result<int>.Success(42);
            var result2 = Result<int>.Success(42);

            // Act & Assert
            Assert.True(result1.Equals(result2));
            Assert.Equal(result1.GetHashCode(), result2.GetHashCode());
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.Equals(object)"/> returns false for two successful results with different values.
        /// </summary>
        [Fact]
        public void Equals_WithDifferentSuccessResults_ReturnsFalse()
        {
            // Arrange
            var result1 = Result<int>.Success(42);
            var result2 = Result<int>.Success(43);

            // Act & Assert
            Assert.False(result1.Equals(result2));
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.Equals(object)"/> returns true for two failed results with the same error message.
        /// </summary>
        [Fact]
        public void Equals_WithSameFailureResult_ReturnsTrue()
        {
            // Arrange
            var result1 = Result<int>.Failure("Error");
            var result2 = Result<int>.Failure("Error");

            // Act & Assert
            Assert.True(result1.Equals(result2));
            Assert.Equal(result1.GetHashCode(), result2.GetHashCode());
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.Equals(object)"/> returns false for two failed results with different error messages.
        /// </summary>
        [Fact]
        public void Equals_WithDifferentFailureResults_ReturnsFalse()
        {
            // Arrange
            var result1 = Result<int>.Failure("Error1");
            var result2 = Result<int>.Failure("Error2");

            // Act & Assert
            Assert.False(result1.Equals(result2));
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.Equals(object)"/> returns false for a success and a failure result.
        /// </summary>
        [Fact]
        public void Equals_WithSuccessAndFailureResults_ReturnsFalse()
        {
            // Arrange
            var result1 = Result<int>.Success(42);
            var result2 = Result<int>.Failure("Error");

            // Act & Assert
            Assert.False(result1.Equals(result2));
        }

        /// <summary>
        /// Verifies that <see cref="Result{T}.Equals(object)"/> returns false when compared to null.
        /// </summary>
        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            // Arrange
            var result = Result<int>.Success(42);

            // Act & Assert
            Assert.False(result.Equals(null));
        }
        #endregion

        #region Non-Generic Result Tests
        /// <summary>
        /// Verifies that <see cref="Result.Success"/> creates a successful non-generic result.
        /// </summary>
        [Fact]
        public void NonGenericResult_Success_CreatesSuccessResult()
        {
            // Arrange & Act
            var result = Result.Success();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Equal(Unit.Value, result.Value);
            Assert.Null(result.ErrorMessage);
        }

        /// <summary>
        /// Verifies that <see cref="Result.Failure(string)"/> creates a failed non-generic result with the specified error message.
        /// </summary>
        [Fact]
        public void NonGenericResult_Failure_CreatesFailureResult()
        {
            // Arrange & Act
            var result = Result.Failure("Something went wrong");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailure);
            Assert.Equal("Something went wrong", result.ErrorMessage);
        }

        /// <summary>
        /// Verifies that <see cref="Result.Failure(string)"/> throws <see cref="ArgumentNullException"/> when passed a null error message.
        /// </summary>
        [Fact]
        public void NonGenericResult_Failure_WithNullErrorMessage_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
        }
        #endregion
    }
}