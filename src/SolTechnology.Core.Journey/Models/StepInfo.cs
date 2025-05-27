using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace SolTechnology.Core.Journey.Models;

public class StepInfo
{
    public string? StepId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public FlowStatus Status { get; set; }
    public List<DataField> RequiredData { get; set; } = new();
    public JsonElement? ProvidedData { get; set; }
}

   public static class SchemaBuilder
    {
        /// <summary>
        /// Recursively builds a list of DataField descriptors for all public properties of the given type.
        /// </summary>
        public static List<DataField> GetSchema(Type type)
        {
            return type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(prop => BuildField(prop.PropertyType, prop.Name))
                .ToList();
        }

        private static DataField BuildField(Type propertyType, string propertyName)
        {
            var isSimple = IsSimpleType(propertyType);
            var field = new DataField
            {
                Name = propertyName,
                Type = GetFriendlyTypeName(propertyType),
                IsComplex = !isSimple
            };

            if (!isSimple)
            {
                // If it's a collection, inspect the element type
                if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType.IsGenericType)
                {
                    var elementType = propertyType.GetGenericArguments().First();
                    // Represent children as the element's schema
                    field.Children = GetSchema(elementType);
                }
                else
                {
                    // Complex object: drill into its properties
                    field.Children = GetSchema(propertyType);
                }
            }

            return field;
        }

        private static bool IsSimpleType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            return 
                underlying.IsPrimitive
                || underlying.IsEnum
                || underlying == typeof(string)
                || underlying == typeof(decimal)
                || underlying == typeof(DateTime)
                || underlying == typeof(Guid);
        }

        private static string GetFriendlyTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var baseName = type.Name.Substring(0, type.Name.IndexOf('`'));
                var args = type.GetGenericArguments().Select(GetFriendlyTypeName);
                return $"{baseName}<{string.Join(", ", args)}>";
            }

            return type.Name;
        }
    }