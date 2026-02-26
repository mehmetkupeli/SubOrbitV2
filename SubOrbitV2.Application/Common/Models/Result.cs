namespace SubOrbitV2.Application.Common.Models;

public class Result
{
    public bool IsSuccess { get; protected set; }
    public string Message { get; protected set; } = string.Empty;
    public List<string> Errors { get; protected set; } = new();

    protected Result() { }

    public static Result Success(string message = "") => new() { IsSuccess = true, Message = message };
    public static Result Failure(string message) => new() { IsSuccess = false, Message = message };
    public static Result Failure(List<string> errors, string message = "Hata oluştu.")
        => new() { IsSuccess = false, Message = message, Errors = errors };
}

public class Result<T> : Result
{
    public T? Data { get; protected set; }

    protected Result() { }

    public static Result<T> Success(T data, string message = "")
        => new() { IsSuccess = true, Data = data, Message = message };

    public new static Result<T> Failure(string message)
        => new() { IsSuccess = false, Message = message };

    public new static Result<T> Failure(List<string> errors, string message = "Hata oluştu.")
        => new() { IsSuccess = false, Message = message, Errors = errors };
}