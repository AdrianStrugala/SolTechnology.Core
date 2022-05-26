namespace SolTechnology.Core.Guards.Context
{
    public class StringContext
    {
        internal string Value { get; }

        internal string Name { get; }

        public StringContext(string value, string name)
        {
            Value = value;
            Name = name;
        }
    }
}
