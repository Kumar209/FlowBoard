namespace SharedKernel;

/// <summary>
/// Result - functional error handling without exceptions. IsSuccess/IsFailure + Error string. Success() for happy path, Failure(error) for domain validation (e.g., Email already exists). Used by MediatR handlers to return Result<AuthResponse> instead of throwing.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

/// <summary>
/// Result<T> - typed version carrying Value (e.g., Result of AuthResponse, Project). Static Success(value) or Failure(error). Keeps controllers thin (return BadRequest(result.Error) vs try/catch).
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T? value, bool isSuccess, string? error) : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true, null);
    public static new Result<T> Failure(string error) => new(default, false, error);
}
