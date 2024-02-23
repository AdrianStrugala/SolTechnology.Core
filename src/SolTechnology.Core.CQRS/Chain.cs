namespace SolTechnology.Core.CQRS
{

    public static class Chain2
    {
        public static Chain2<TContext> Start<TContext>(TContext context, CancellationToken cancellationToken)
        {
            return new Chain2<TContext>(context, cancellationToken);
        }
    }


    public class Chain2<TContext>
    {
        private readonly CancellationToken _cancellationToken;
        private readonly List<Exception> _exceptions = new();
        internal TContext Context { get; }



        public Chain2(TContext context, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            Context = context;
        }

        public async Task<Chain2<TContext>> Then(Func<TContext, Task<OperationResult>> func)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (_exceptions.Any())
            {
                return this;
            }

            var operationResult = new OperationResult();
            try
            {
                operationResult = await func.Invoke(Context);
            }
            catch (Exception e)
            {
                _exceptions.Add(e);
            }
            if (operationResult.IsFailure)
            {
                _exceptions.Add(new Exception(operationResult.ErrorMessage));
            }
            return this;
        }

        public async Task<Chain2<TContext>> Then(Func<TContext, Task> func)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (_exceptions.Any())
            {
                return this;
            }

            var operationResult = new OperationResult();
            try
            {
                await func.Invoke(Context);
            }
            catch (Exception e)
            {
                _exceptions.Add(e);
            }
            if (operationResult.IsFailure)
            {
                _exceptions.Add(new Exception(operationResult.ErrorMessage));
            }
            return this;
        }

        public OperationResult<TResult> End<TResult>(Func<TContext, TResult> func)
        {
            if (_exceptions.Any())
            {
                var errorMessage = string.Join(string.Empty, _exceptions.Select(x =>
                    $"{Environment.NewLine} -- {x.Message}"));
                return OperationResult<TResult>.Failed(errorMessage);
            }
            return OperationResult<TResult>.Succeeded(func.Invoke(Context));
        }
    }

    public static class Chain2Extensions
    {
        public static async Task<Chain2<TContext>> Then<TContext>(
            this Task<Chain2<TContext>> asyncChain,
            Func<TContext, Task<OperationResult>> action)
        {
            var chain = await asyncChain;
            return await chain.Then(action);
        }

        public static async Task<Chain2<TContext>> Then<TContext>(
            this Task<Chain2<TContext>> asyncChain,
            Func<TContext, Task> action)
        {
            var chain = await asyncChain;
            return await chain.Then(action);
        }

        public static async Task<OperationResult<TResult>> End<TContext, TResult>(
            this Task<Chain2<TContext>> asyncChain,
            Func<TContext, TResult> action)
        {
            var chain = await asyncChain;
            return chain.End(action);
        }
    }
}
