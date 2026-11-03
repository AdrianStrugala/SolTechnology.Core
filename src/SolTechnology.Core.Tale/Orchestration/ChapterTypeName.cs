namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Version-free full type name used in the chapter history key: namespace and name, generic
/// arguments resolved recursively, and no assembly information, so a redeploy keeps the key.
/// </summary>
internal static class ChapterTypeName
{
    public static string Of(Type type)
    {
        if (type.IsArray)
        {
            return $"{Of(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
        }

        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        var arguments = string.Join(",", type.GetGenericArguments().Select(Of));
        return $"{type.GetGenericTypeDefinition().FullName}[{arguments}]";
    }
}
