namespace FileFlows.Common;

/// <summary>
/// Represents a result type that can either hold a value or an error message.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
public readonly struct Result<TValue>
{
    private readonly TValue _value;
    private readonly string _error;

    /// <summary>
    /// Gets the value
    /// </summary>
    public TValue Value => _value!;
    
    /// <summary>
    /// Gets the value or the types default value
    /// </summary>
    public TValue? ValueOrDefault => _value ?? default(TValue);

    /// <summary>
    /// Gets the error of the failure
    /// </summary>
    public string? Error => IsFailed ? _error : null;

    /// <summary>
    /// Indicates whether the result failed.
    /// </summary>
    public bool IsFailed { get; }

    /// <summary>
    /// Private constructor to create a Result instance.
    /// </summary>
    /// <param name="isFailed">Indicates whether the result failed.</param>
    /// <param name="value">The value stored in the result.</param>
    /// <param name="error">The error message stored in the result.</param>
    private Result(bool isFailed, TValue value, string error)
    {
        IsFailed = isFailed;
        _value = value;
        _error = error;
    }

    /// <summary>
    /// Creates a successful result with the provided value.
    /// </summary>
    /// <param name="value">The value to be stored in the result.</param>
    /// <returns>A successful result with the provided value.</returns>
    public static Result<TValue> Success(TValue value) => new Result<TValue>(false, value, null!);

    /// <summary>
    /// Creates a failed result with the provided error message.
    /// </summary>
    /// <param name="error">The error message to be stored in the result.</param>
    /// <returns>A failed result with the provided error message.</returns>
    public static Result<TValue> Fail(string error) => new Result<TValue>(true, default!, error);

    
    /// <summary>
    /// Implicitly converts a value into a Result of TValue.
    /// </summary>
    /// <param name="value">The value to be converted.</param>
    public static implicit operator Result<TValue>(TValue value) => new(false, value, null!);

    /// <summary>
    /// Implicitly converts a value into a Result of TValue.
    /// </summary>
    /// <param name="value">The value to be converted.</param>
    public static implicit operator TValue(Result<TValue> value)
    {
        if (value.IsFailed == false)
            return value.Value;
        if (string.IsNullOrEmpty(value._error) == false)
            throw new Exception(value._error);
        throw new InvalidOperationException("Cannot cast an error Result to TValue.");
    }

    /// <summary>
    /// Matches the result and applies the provided functions based on whether it's a value or an error.
    /// </summary>
    /// <typeparam name="TResult">The type of the result after applying the functions.</typeparam>
    /// <param name="value">The function to be applied if it's a value.</param>
    /// <param name="error">The function to be applied if it's an error.</param>
    /// <returns>The result after applying the function.</returns>
    public TResult Match<TResult>(Func<TValue, TResult> value, Func<string, TResult> error)
        => IsFailed ? error(_error!) : value(_value!);

    /// <summary>
    /// Checks if the provided value matches the value stored in the result.
    /// </summary>
    /// <param name="value">The value to compare with the stored result value.</param>
    /// <returns>True if the provided value matches the stored result value; otherwise, false.</returns>
    public bool Is(TValue value)
    {
        if (IsFailed)
            return false;

        if (Equals(value, _value)) // Using Equals to handle null values
            return true;

        return false;
    }

    /// <summary>
    /// If the result failed or not
    /// </summary>
    /// <param name="error">the error if failed, otherwise null</param>
    /// <returns>true if failed, false if did not fail</returns>
    public bool Failed(out string error)
    {
        error = IsFailed ? _error : string.Empty;
        return IsFailed;
    }
    
    /// <summary>
    /// If the result succeeded or not
    /// </summary>
    /// <param name="value">the value if passed, otherwise the default of TValue</param>
    /// <returns>true if passed, false if did not fail</returns>
    public bool Success(out TValue value)
    {
        value = IsFailed ? default! : _value;
        return IsFailed == false;
    }
}