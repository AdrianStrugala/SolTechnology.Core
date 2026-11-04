using System.Text.Json;

namespace SolTechnology.Core.Cache;

/// <summary>
/// Builds cache keys namespaced by the key and item types, so two call sites that share a key value
/// never share an entry. Type names carry no assembly information, so a redeploy keeps the keys.
/// </summary>
internal static class CacheKey
{
    public static string For<TKey, TItem>(TKey key, JsonSerializerOptions? serializerOptions = null) =>
        $"{TypeName(typeof(TKey))}:{TypeName(typeof(TItem))}:{JsonSerializer.Serialize(key, serializerOptions)}";

    internal static string TypeName(Type type)
    {
        if (type.IsArray)
        {
            return $"{TypeName(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
        }

        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        var arguments = string.Join(",", type.GetGenericArguments().Select(TypeName));
        return $"{type.GetGenericTypeDefinition().FullName}[{arguments}]";
    }
}
