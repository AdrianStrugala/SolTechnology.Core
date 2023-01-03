using SolTechnology.Core.CQRS.ResultPattern;

namespace SolTechnology.Core.CQRS.ChainPattern
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
            var result = await asyncFunc(chain.Value).ConfigureAwait(true);
            return new Chain<TOut>(result);
        }

        public static async Task<Chain<TOut>> Then<TIn, TOut>(
            this Task<Chain<TIn>> asyncChain,
            Func<TIn, TOut> func)
        {
            var chain = await asyncChain;
            return chain.Then(func);
        }

        public static async Task<Chain<Task>> Then<TIn>(
            this Task<Chain<TIn>> asyncChain,
            Func<TIn, Task> func)
        {
            var chain = await asyncChain;
            return chain.Then(func);
        }

        public static async Task<Chain<TOut>> Then<TIn, TOut>(
            this Task<Chain<TIn>> asyncChain,
            Func<TIn, Task<TOut>> asyncFunc)
        {
            var chain = await asyncChain;
            return await chain.Then(asyncFunc);
        }

        public static async Task<Chain<T>> Then<T>(
                this Task<Chain<T>> asyncChain,
                Action<T> action)
        {
            var chain = await asyncChain;
            return chain.Then(action);
        }

        public static Chain<T> Then<T>(
            this Chain<T> chain,
            Action<T> action)
        {
            action(chain.Value);
            return chain;
        }

        public static async Task<Result<Vacuum>> EndCommand<T>(
            this Task<Chain<T>> asyncChain)
        {
            await asyncChain;
            return Result<Vacuum>.Success();
        }

        public static Task<Result<Vacuum>> EndCommand<T>(
            this Chain<T> chain)
        {
            return Task.FromResult(Result<Vacuum>.Success());
        }

        public static async Task<Result<T>> EndQuery<T>(
            this Task<Chain<T>> asyncChain)
        {
            var chain = await asyncChain;
            return Result<T>.Success(chain.Value);
        }

        public static Task<Result<T>> EndQuery<T>(
            this Chain<T> chain)
        {
            return Task.FromResult(Result<T>.Success(chain.Value));
        }
    }
}
