namespace SolTechnology.Core.CQRS
{

    public static class Chain
    {
        public static Chain<TContext> Start<TContext>(TContext context, CancellationToken cancellationToken)
        {
            return new Chain<TContext>(context, cancellationToken);
        }
    }


    public class Chain<TContext>
    {
        private readonly CancellationToken _cancellationToken;
        private readonly List<Exception> _exceptions = new();
        internal TContext Context { get; }



        public Chain(TContext context, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            Context = context;
        }

        public async Task<Chain<TContext>> Then(Func<TContext, Task<Result>> func)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (_exceptions.Any())
            {
                return this;
            }

            Result operationResult;
            try
            {
                operationResult = await func.Invoke(Context);
            }
            catch (Exception e)
            {
                _exceptions.Add(e);
                return this;
            }
            if (operationResult.IsFailure)
            {
                _exceptions.Add(new Exception(operationResult.Error?.Message));
            }
            return this;
        }

        public async Task<Chain<TContext>> Then(Func<TContext, Task> func)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (_exceptions.Any())
            {
                return this;
            }

            try
            {
                await func.Invoke(Context);
            }
            catch (Exception e)
            {
                _exceptions.Add(e);
                return this;
            }

            return this;
        }

        public Task<Chain<TContext>> Then(Action<TContext> func)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (_exceptions.Any())
            {
                return Task.FromResult(this);
            }

            try
            {
                func.Invoke(Context);
            }
            catch (Exception e)
            {
                _exceptions.Add(e);
            }
           
            return Task.FromResult(this);
        }

        public Result<TResult> End<TResult>(Func<TContext, TResult> func)
        {
            if (_exceptions.Any())
            {
                var errorMessage = string.Join(string.Empty, _exceptions.Select(x =>
                    $"{Environment.NewLine} -- {x.Message}"));
                return Result<TResult>.Fail(errorMessage);
            }
            return func.Invoke(Context);
        }

        public Result End()
        {
            return Result.Success();
        }
    }

    public static class ChainExtensions
    {
        public static async Task<Chain<TContext>> Then<TContext>(
            this Task<Chain<TContext>> asyncChain,
            Func<TContext, Task<Result>> action)
        {
            var chain = await asyncChain;
            return await chain.Then(action);
        }

        public static async Task<Chain<TContext>> Then<TContext>(
            this Task<Chain<TContext>> asyncChain,
            Func<TContext, Task> action)
        {
            var chain = await asyncChain;
            return await chain.Then(action);
        }

        public static async Task<Chain<TContext>> Then<TContext>(
            this Task<Chain<TContext>> asyncChain,
            Action<TContext> action)
        {
            var chain = await asyncChain;
            return await chain.Then(action);
        }

        public static async Task<Result<TResult>> End<TContext, TResult>(
            this Task<Chain<TContext>> asyncChain,
            Func<TContext, TResult> action)
        {
            var chain = await asyncChain;
            return chain.End(action);
        }

        public static async Task<Result> End<TContext>(
            this Task<Chain<TContext>> asyncChain)
        {
            var chain = await asyncChain;
            return chain.End();
        }
    }
}
