namespace CustomerService.Domain.Shared;
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public string? Message { get; }
    public int StatusCode { get; }

    protected Result(bool isSuccess, string? error, string? message, int statusCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        Message = message;
        StatusCode = statusCode;
    }

    public static Result Success(string? message = null) => new(true, null, message, 200);
    public static Result Created(string? message = null) => new(true, null, message, 201);
    public static Result Failure(string error) => new(false, error, null, 400);
    public static Result NotFound(string error) => new(false, error, null, 404);
    public static Result Validation(string error) => new(false, error, null, 400);
    public static Result<T> Success<T>(T value, string? message = null) => new(value, true, null, message, 200);
    public static Result<T> Created<T>(T value, string? message = null) => new(value, true, null, message, 201);
    public static Result<T> Failure<T>(string error) => new(default, false, error, null, 400);
    public static Result<T> NotFound<T>(string error) => new(default, false, error, null, 404);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    public Result(T? value, bool isSuccess, string? error, string? message, int statusCode) : base(isSuccess, error, message, statusCode) => Value = value;
    public static Result<T> Success(T value, string? message = null) => new(value, true, null, message, 200);
    public static Result<T> Created(T value, string? message = null) => new(value, true, null, message, 201);
    public static Result<T> Failure(string error) => new(default, false, error, null, 400);
    public static Result<T> NotFound(string error) => new(default, false, error, null, 404);
}

public static class Errors
{
    public static Result<T> NotFound<T>(string message) => Result<T>.NotFound(message);
}