namespace SolTechnology.Core.Guards
{
    public class StringGuards
    {
        internal string Value { get; set; }

        internal string Name { get; set; }

        internal List<string> Errors { get; set; }

        public StringGuards(string value, string name, List<string> errors)
        {
            Value = value;
            Name = name;
            Errors = errors;
        }
    }



    public static class StringGuardsExtensions
    {
        public static StringGuards NotNull(this StringGuards guards)
        {
            if (guards.Value == null)
            {
                guards.Errors.Add($"String [{guards.Name}] cannot be null!");
            }

            return guards;
        }

        public static StringGuards NotEmpty(this StringGuards guards)
        {
            if (guards.Value == string.Empty)
            {
                guards.Errors.Add($"String [{guards.Name}] cannot be empty!");
            }

            return guards;
        }

        public static StringGuards NotWhitespace(this StringGuards guards)
        {
            if (guards.Value != null && string.IsNullOrWhiteSpace(guards.Value))
            {
                guards.Errors.Add($"String [{guards.Name}] cannot be whitespace!");
            }

            return guards;
        }

        public static StringGuards Equal(this StringGuards guards, string toCompare)
        {
            if (guards.Value != toCompare)
            {
                guards.Errors.Add($"String [{guards.Value}] of name [{guards.Name}] has to be equal [{toCompare}]!");
            }

            return guards;
        }

        public static StringGuards NotEqual(this StringGuards guards, string toCompare)
        {
            if (guards.Value == toCompare)
            {
                guards.Errors.Add($"String [{guards.Value}] of name [{guards.Name}] cannot be equal [{toCompare}]!");
            }

            return guards;
        }
    }
}
