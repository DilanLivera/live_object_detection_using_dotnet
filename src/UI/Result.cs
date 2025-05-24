namespace UI;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error message.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public class Result<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value if the operation was successful.
    /// </summary>
    public T? Value { get; private init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value to return on success.</param>
    /// <returns>A successful Result containing the value.</returns>
    public static Result<T> Success(T value) => new()
                                                {
                                                    IsSuccess = true, Value = value
                                                };

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed Result containing the error message.</returns>
    public static Result<T> Failure(string errorMessage) => new()
                                                            {
                                                                IsSuccess = false, ErrorMessage = errorMessage
                                                            };

    /// <summary>
    /// Implicitly converts a value to a successful Result.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A successful Result containing the value.</returns>
    public static implicit operator Result<T>(T value) => Success(value);

}