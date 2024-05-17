using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;
using System.Diagnostics;

namespace SolTechnology.Core.CQRS.Decorators.Logging
{
    public class QueryHandlerLoggingDecorator<TQuery, TResult> : IQueryHandler<TQuery, TResult>
    {
        private readonly IQueryHandler<TQuery, TResult> _handler;
        private readonly ILogger<ICommandHandler<TQuery, TResult>> _logger;

        public QueryHandlerLoggingDecorator(
            IQueryHandler<TQuery, TResult> handler,
            ILogger<ICommandHandler<TQuery, TResult>> logger)
        {
            _handler = handler;
            _logger = logger;
        }

        public async Task<Result<TResult>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            string operationName;

            if (query is ILoggableOperation loggedOperation)
            {
                using var scope = _logger.BeginOperationScope(new KeyValuePair<string, object>(
                    loggedOperation.LogScope.OperationIdName,
                    loggedOperation.LogScope.OperationId));

                operationName = loggedOperation.LogScope.OperationName;
            }
            else
            {
                operationName = typeof(TQuery).FullName;
            }

            var sw = Stopwatch.StartNew();
            _logger.OperationStarted(operationName);

            try
            {
                var result = await _handler.Handle(query, cancellationToken);
                _logger.OperationSucceeded(operationName, sw.ElapsedMilliseconds);

                return result;
            }
            catch (Exception e)
            {
                _logger.OperationFailed(operationName, sw.ElapsedMilliseconds, e);
                throw;
            }
        }
    }
}
