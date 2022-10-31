namespace SolTechnology.Core.CQRS
{

    public class Chain
    {
        public static Chain<T> Start<T>(Func<T> func)
        {
            return new Chain<T>(func());
        }

        public static async Task<Chain<T>> Start<T>(Func<Task<T>> func)
        {
            return new Chain<T>(await func());
        }
    }

    public class Chain<T>
    {
        public readonly T Value;

        public Chain(T value)
        {
            Value = value;
        }
    }
}
