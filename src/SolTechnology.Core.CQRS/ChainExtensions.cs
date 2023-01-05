namespace SolTechnology.Core.CQRS
{
    public static class ChainExtensions
    {
        public static Chain<TOut> Then<TIn, TOut>(
                this Chain<TIn> chain,
                Func<TIn, TOut> func)
        {
            return new Chain<TOut>(func(chain.Value));
        }

        public static async Task<Chain<TOut>> Then<TIn, TOut>(
            this Chain<TIn> chain,
            Func<TIn, Task<TOut>> asyncFunc)
        {
            var result = await asyncFunc(chain.Value);
            return new Chain<TOut>(result);
        }

        public static async Task<Chain<TOut>> Then<TIn, TOut>(
            this Task<Chain<TIn>> asyncChain,
            Func<TIn, TOut> func)
        {
            var chain = await asyncChain;
            return Then(chain, func);
        }

        public static async Task<Chain<Task>> Then<TIn>(
            this Task<Chain<TIn>> asyncChain,
            Func<TIn, Task> func)
        {
            var chain = await asyncChain;
            return Then(chain, func);
        }

        public static async Task<Chain<TOut>> Then<TIn, TOut>(
            this Task<Chain<TIn>> asyncChain,
            Func<TIn, Task<TOut>> asyncFunc)
        {
            var chain = await asyncChain;
            return await Then(chain, asyncFunc);
        }

        public static async Task<Chain<T>> Then<T>(
                this Task<Chain<T>> asyncChain,
                Action<T> action)
        {
            var chain = await asyncChain;
            return chain.Then<T>(action);
        }

        public static Chain<T> Then<T>(
            this Chain<T> chain,
            Action<T> action)
        {
            action(chain.Value);
            return chain;
        }

        public static async Task EndCommand<TIn>(
            this Task<Chain<TIn>> asyncChain)
        {
            await asyncChain;
        }

        public static Task EndCommand<TIn>(
            this Chain<TIn> chain)
        {
            return Task.FromResult(chain);
        }

        public static async Task<TIn> EndQuery<TIn>(
            this Task<Chain<TIn>> asyncChain)
        {
            var chain = await asyncChain;
            return chain.Value;
        }

        public static Task EndQuery<TIn>(
            this Chain<TIn> chain)
        {
            return Task.FromResult(chain.Value);
        }
    }
}
