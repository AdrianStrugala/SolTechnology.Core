using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using SolTechnology.Core.Logging;

namespace SolTechnology.Core.CQRS.PipelineBehaviors;

/// <summary>
/// One <c>[LogScope]</c>-marked property of a request, compiled into a fast getter.
/// </summary>
internal sealed record LogScopeBinding(string Key, Func<object, object?> Getter);

/// <summary>
/// Per-request-type cache of compiled <c>[LogScope]</c> getters. Reflection happens at most
/// once per <c>TRequest</c>; subsequent calls go through the cached delegate.
/// </summary>
internal static class LogScopeBindingCache
{
    private static readonly ConcurrentDictionary<Type, LogScopeBinding[]> Cache = new();

    public static LogScopeBinding[] GetBindings(Type requestType)
        => Cache.GetOrAdd(requestType, BuildBindings);

    private static LogScopeBinding[] BuildBindings(Type requestType)
    {
        var properties = requestType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var bindings = new List<LogScopeBinding>(properties.Length);

        foreach (var property in properties)
        {
            if (!property.CanRead)
            {
                continue;
            }

            var attribute = property.GetCustomAttribute<LogScopeAttribute>(inherit: true);
            if (attribute is null)
            {
                continue;
            }

            var key = string.IsNullOrWhiteSpace(attribute.Name) ? property.Name : attribute.Name!;
            var getter = BuildGetter(requestType, property);
            bindings.Add(new LogScopeBinding(key, getter));
        }

        return bindings.Count == 0 ? Array.Empty<LogScopeBinding>() : bindings.ToArray();
    }

    /// <summary>
    /// Compiles <c>(object instance) =&gt; (object?)((TRequest)instance).Property</c> so reads
    /// don't go through <see cref="PropertyInfo.GetValue(object?)"/> reflection on every request.
    /// </summary>
    private static Func<object, object?> BuildGetter(Type requestType, PropertyInfo property)
    {
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var typedInstance = Expression.Convert(instanceParam, requestType);
        var propertyAccess = Expression.Property(typedInstance, property);
        var boxed = Expression.Convert(propertyAccess, typeof(object));
        return Expression.Lambda<Func<object, object?>>(boxed, instanceParam).Compile();
    }
}

