namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Version-free full type name, the default recurring job id when it fits (see <see cref="StorageKey"/>): namespace and name, generic arguments
/// resolved recursively, and no assembly information, so a redeploy keeps the id.
/// </summary>
internal static class JobTypeName
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
