#nullable enable
using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.CQRS;

/// <summary>
/// Functional combinators for <see cref="Result{T}"/>.
/// </summary>
public static class ResultExtensions
{
    /// <summary>Projects the data if successful; short-circuits on failure.</summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map)
        => result.IsFailure ? Result<TOut>.Fail(result.Error!) : Result<TOut>.Success(map(result.Data!));

    /// <summary>Async projection.</summary>
    public static async Task<Result<TOut>> Map<TIn, TOut>(this Task<Result<TIn>> task, Func<TIn, TOut> map)
        => (await task).Map(map);

    /// <summary>Chains an operation that itself returns a Result; short-circuits on failure.</summary>
    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> bind)
        => result.IsFailure ? Result<TOut>.Fail(result.Error!) : await bind(result.Data!);

    /// <summary>Async bind.</summary>
    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Task<Result<TIn>> task, Func<TIn, Task<Result<TOut>>> bind)
        => await (await task).Bind(bind);

    /// <summary>Executes a side-effect on success without changing the result.</summary>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess) action(result.Data!);
        return result;
    }

    /// <summary>Async tap.</summary>
    public static async Task<Result<T>> Tap<T>(this Result<T> result, Func<T, Task> action)
    {
        if (result.IsSuccess) await action(result.Data!);
        return result;
    }

    /// <summary>Pattern-match success/failure into a single value.</summary>
    public static TOut Match<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> onSuccess, Func<Error, TOut> onFailure)
        => result.IsSuccess ? onSuccess(result.Data!) : onFailure(result.Error!);

    /// <summary>Validates a condition on success; returns failure if the predicate fails.</summary>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error onFailure)
        => result.IsFailure ? result : predicate(result.Data!) ? result : Result<T>.Fail(onFailure);

    /// <summary>Async ensure.</summary>
    public static async Task<Result<T>> Ensure<T>(this Task<Result<T>> task, Func<T, bool> predicate, Error onFailure)
        => (await task).Ensure(predicate, onFailure);
}

