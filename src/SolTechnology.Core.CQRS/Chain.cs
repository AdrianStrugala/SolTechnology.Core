namespace SolTechnology.Core.CQRS
{
    public class Chain<T>
    {
        public readonly T Value;

        public Chain()
        {
        }

        public Chain(T value)
        {

            Value = value;
        }
    }
}
