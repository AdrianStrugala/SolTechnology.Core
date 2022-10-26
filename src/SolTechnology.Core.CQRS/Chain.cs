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

    public class ChainBuilders
    {
        public Chain<T> Chain<T>(Func<T> func)
        {
            return new Chain<T>(func());
        }

        public async Task<Chain<T>> Chain<T>(Func<Task<T>> func)
        {
            return new Chain<T>(await func());
        }
    }
}
