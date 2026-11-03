using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using SolTechnology.Core.AUID;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Orchestration;

namespace SolTechnology.Core.Tale.Api;

/// <summary>
/// Calls the generic <see cref="TaleManager"/> methods for a handler type known only at run time,
/// closing them over the handler's input, context and output types. Method handles are cached.
/// </summary>
internal static class TaleManagerInvoker
{
    private static readonly ConcurrentDictionary<string, MethodInfo> Methods = new();

    public static bool TryGetGenericArgs(Type handlerType, out Type[] args)
    {
        for (var type = handlerType.BaseType; type != null && type != typeof(object); type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition().Name == "TaleHandler`3")
            {
                args = type.GetGenericArguments();
                return true;
            }
        }

        args = [];
        return false;
    }

    public static Task<Result<TaleInstance>> StartStory(
        TaleManager manager, Type handlerType, Type[] args, object input, string? idempotencyKey, CancellationToken cancellationToken)
        => Invoke(manager, nameof(TaleManager.StartStory), handlerType, args, [input, idempotencyKey, cancellationToken]);

    public static Task<Result<TaleInstance>> ResumeStory(
        TaleManager manager, Type handlerType, Type[] args, Auid taleId, JsonElement? userInput, CancellationToken cancellationToken)
        => Invoke(manager, nameof(TaleManager.ResumeStory), handlerType, args, [taleId, userInput, cancellationToken]);

    private static Task<Result<TaleInstance>> Invoke(
        TaleManager manager, string methodName, Type handlerType, Type[] args, object?[] parameters)
    {
        var method = Methods.GetOrAdd(methodName, name =>
            typeof(TaleManager).GetMethod(name)
            ?? throw new InvalidOperationException($"TaleManager method '{name}' not found."));

        return (Task<Result<TaleInstance>>)method
            .MakeGenericMethod(handlerType, args[0], args[1], args[2])
            .Invoke(manager, parameters)!;
    }
}
