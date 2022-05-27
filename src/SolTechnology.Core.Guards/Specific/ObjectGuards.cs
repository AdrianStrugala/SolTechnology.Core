namespace SolTechnology.Core.Guards
{
    public class ObjectGuards
    {
        internal object Value { get; set; }

        internal string Name { get; set; }

        internal List<string> Errors { get; set; }

        public ObjectGuards(object value, string name, List<string> errors)
        {
            Value = value;
            Name = name;
            Errors = errors;
        }
    }



    public static class ObjectGuardsExtensions
    {
        public static ObjectGuards NotNull(this ObjectGuards guards)
        {
            if (guards.Value == null)
            {
                guards.Errors.Add($"Object [{guards.Name}] cannot be null!");
            }

            return guards;
        }
    }
}
