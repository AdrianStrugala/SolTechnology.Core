using System.Collections.Concurrent;
using System.Linq.Expressions;
using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.CQRS.PipelineBehaviors;

/// <summary>
/// Cached factory that produces <c>Result.Fail(validationError)</c> or
/// <c>Result&lt;T&gt;.Fail(validationError)</c> for a given <typeparamref name="TResponse"/>.
/// </summary>
internal static class ValidationFailureFactory
{
    private static readonly ConcurrentDictionary<Type, Func<ValidationError, object>> Factories = new();

    public static TResponse Create<TResponse>(ValidationError error)
    {
        var factory = Factories.GetOrAdd(typeof(TResponse), Build);
        return (TResponse)factory(error);
    }

    private static Func<ValidationError, object> Build(Type responseType)
    {
        // Result (non-generic)
        if (responseType == typeof(Result))
        {
            return error => Result.Fail(error);
        }

        // Result<T>
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failMethod = responseType.GetMethod("Fail", new[] { typeof(Error) })!;

            var errorParam = Expression.Parameter(typeof(ValidationError), "error");
            var call = Expression.Call(failMethod, Expression.Convert(errorParam, typeof(Error)));
            var boxed = Expression.Convert(call, typeof(object));
            var lambda = Expression.Lambda<Func<ValidationError, object>>(boxed, errorParam);
            return lambda.Compile();
        }

        // Fallback — shouldn't happen with ICommand/IQuery markers
        throw new InvalidOperationException(
            $"Cannot create a validation failure result for response type {responseType.Name}. " +
            "Ensure your request implements ICommand, ICommand<T>, or IQuery<T>.");
    }
}

