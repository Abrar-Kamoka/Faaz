namespace Faaz.SharedKernel.Results;

public sealed class ApiResponse<T>
{
    public bool   Success    { get; init; }
    public int    StatusCode { get; init; }
    public string Message    { get; init; } = string.Empty;
    public T?     Data       { get; init; }
}

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, string message = "Success.")
        => new() { Success = true, StatusCode = 200, Message = message, Data = data };

    public static ApiResponse<T> Created<T>(T data, string message = "Created successfully.")
        => new() { Success = true, StatusCode = 201, Message = message, Data = data };

    public static ApiResponse<object?> NoContent(string message = "Done.")
        => new() { Success = true, StatusCode = 200, Message = message, Data = null };

    public static ApiResponse<object?> Fail(int statusCode, string message)
        => new() { Success = false, StatusCode = statusCode, Message = message, Data = null };

    public static ApiResponse<T> Fail<T>(int statusCode, string message, T data)
        => new() { Success = false, StatusCode = statusCode, Message = message, Data = data };
}
