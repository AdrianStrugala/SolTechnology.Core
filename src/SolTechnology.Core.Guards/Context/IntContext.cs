namespace SolTechnology.Core.Guards.Context;

public class IntContext
{
    internal int Value { get; }

    internal string Name { get; }

    public IntContext(int value, string name)
    {
        Value = value;
        Name = name;
    }
}