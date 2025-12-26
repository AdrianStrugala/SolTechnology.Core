using System.Reflection;

namespace SolTechnology.Core.Story.Models;

/// <summary>
/// Describes a single field in an input schema.
/// Used to communicate to API consumers what data structure is expected for interactive chapters.
/// </summary>
public class DataField
{
    /// <summary>
    /// The name of the field (property name).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The type of the field as a string (e.g., "String", "Int32", "Boolean").
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Indicates whether this field is a complex type (class/struct) with nested properties.
    /// </summary>
    public required bool IsComplex { get; set; }

    /// <summary>
    /// If IsComplex is true, contains the nested fields of this complex type.
    /// Empty list for simple types.
    /// </summary>
    public List<DataField> Children { get; set; } = new();
}

/// <summary>
/// Extension methods for generating DataField schemas from types.
/// </summary>
public static class SchemaBuilder
{
    /// <summary>
    /// Converts a type into a list of DataFields describing its structure.
    /// Uses reflection to introspect public properties.
    /// Recursively processes complex types and collections.
    /// </summary>
    /// <param name="type">The type to generate schema for</param>
    /// <returns>List of DataFields describing the type's public properties</returns>
    public static List<DataField> ToDataFields(this Type type)
    {
        var fields = new List<DataField>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var field = new DataField
            {
                Name = prop.Name,
                Type = GetFriendlyTypeName(prop.PropertyType),
                IsComplex = IsComplexType(prop.PropertyType)
            };

            if (field.IsComplex && !IsCollection(prop.PropertyType))
            {
                // Recursively process complex types (but not collections to avoid infinite loops)
                field.Children = ToDataFields(prop.PropertyType);
            }

            fields.Add(field);
        }

        return fields;
    }

    private static string GetFriendlyTypeName(Type type)
    {
        // Handle nullable types
        if (Nullable.GetUnderlyingType(type) != null)
        {
            return GetFriendlyTypeName(Nullable.GetUnderlyingType(type)!) + "?";
        }

        // Handle collections
        if (IsCollection(type))
        {
            var elementType = type.IsArray
                ? type.GetElementType()!
                : type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

            return $"List<{GetFriendlyTypeName(elementType)}>";
        }

        // Handle generic types
        if (type.IsGenericType)
        {
            var genericTypeName = type.Name.Split('`')[0];
            var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
            return $"{genericTypeName}<{genericArgs}>";
        }

        return type.Name;
    }

    private static bool IsComplexType(Type type)
    {
        // Unwrap nullable
        if (Nullable.GetUnderlyingType(type) != null)
        {
            type = Nullable.GetUnderlyingType(type)!;
        }

        // Simple types
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
            type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(DateOnly) ||
            type == typeof(TimeOnly))
        {
            return false;
        }

        // Collections are not considered "complex" for schema purposes (to avoid deep recursion)
        if (IsCollection(type))
        {
            return false;
        }

        // Enums
        if (type.IsEnum)
        {
            return false;
        }

        // Everything else is complex
        return true;
    }

    private static bool IsCollection(Type type)
    {
        return type.IsArray ||
               (type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof(List<>) ||
                 type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                 type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                 type.GetGenericTypeDefinition() == typeof(IList<>)));
    }
}
