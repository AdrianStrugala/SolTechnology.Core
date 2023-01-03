namespace SolTechnology.Core.CQRS.ResultPattern;

public class Result<T>
{
    public T Value { get; }
    public Exception Exception { get; }
    public ResultTypes Type { get; }


    public bool IsSuccess => Type == ResultTypes.Success;

    private Result(ResultTypes type, T value, Exception exception = default)
    {
        Value = value;
        Exception = exception;
        Type = type;
    }

    public static Result<T> Failure(T defaultValue = default, Exception exception = default) => new(ResultTypes.Failure, defaultValue, exception);
    public static Result<T> Success(T value = default) => new(ResultTypes.Success, value);
}
