namespace SolTechnology.Core.Guards
{
    public class GuardsContext<T>
    {
        internal T Value { get; set; }

        internal string Name { get; set; }

        public GuardsContext(T value, string name)
        {
            Value = value;
            Name = name;
        }
    }
}
