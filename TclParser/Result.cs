namespace TclParser;

public interface IResult
{
    bool IsSuccess { get; }
    string Message { get; }
}


public interface IResult<out T> : IResult
{
    T? Data { get; }
}


public class Result<T> : IResult<T>
{
    private const string OkMessage = "Ok";
    private const string ErrorMessage = "Error";
    
    public bool IsSuccess { get; }
    public string Message { get; }
    public T? Data { get; }

   
    private Result(bool isSuccess, T? data = default, string message = "")
    {
        IsSuccess = isSuccess;
        Data = data;
        Message = message;
    }
    

    public static IResult<T> Ok(string? message = default)
        => new Result<T>(true, default, message ?? OkMessage);
    

    public static IResult<T> Ok(T? data, string? message = default)
        => new Result<T>(true, data, message ?? OkMessage);
        

    public static IResult<T> Error(string? message = default)
        => new Result<T>(false, default, message ?? ErrorMessage);
    

    public static IResult<T> Error(T? data, string? message = default)
        => new Result<T>(false, data, message ?? ErrorMessage);
        
    
    public static IResult<Exception> Error(Exception ex, string? message = default)
        => new Result<Exception>(false, ex, message ?? ex.Message);
}


public static class SimpleResult
{
    public static IResult Ok(string? message = default)
        => Result<object>.Ok(message);
    

    public static IResult Ok(object? data, string? message = default)
        => Result<object>.Ok(data, message);


    public static IResult Error(string? message = default)
        => Result<object>.Error(message);
    

    public static IResult Error(object? data, string? message = default)
        => Result<object>.Error(data, message);


    public static IResult Error(Exception ex, string? message = default)
        => Result<object>.Error(ex, message);
    

    public static IResult FromBoolean(bool state, string? message = default)
        => state
            ? Ok(message)
            : Error(message);
}
