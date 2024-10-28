using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;
using System.Diagnostics;
using MediatR;

namespace SolTechnology.Core.CQRS.Decorators.Logging
{
    public class CommandHandlerLoggingDecorator<TCommand> : ICommandHandler<TCommand> where TCommand : IRequest, IRequest<Result>
    {
        private readonly ICommandHandler<TCommand> _handler;
        private readonly ILogger<ICommandHandler<TCommand>> _logger;

        public CommandHandlerLoggingDecorator(
            ICommandHandler<TCommand> handler,
            ILogger<ICommandHandler<TCommand>> logger)
        {
            _handler = handler;
            _logger = logger;
        }

        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken = default)
        {
            string operationName;

            if (command is ILoggableOperation loggedOperation)
            {
                using var scope = _logger.BeginOperationScope(new KeyValuePair<string, object>(
                    loggedOperation.LogScope.OperationIdName,
                    loggedOperation.LogScope.OperationId));

                operationName = loggedOperation.LogScope.OperationName;
            }
            else
            {
                operationName = typeof(TCommand).FullName;
            }

            var sw = Stopwatch.StartNew();
            _logger.OperationStarted(operationName);

            try
            {
                var result = await _handler.Handle(command, cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.OperationSucceeded(operationName, sw.ElapsedMilliseconds);
                }
                else
                {
                    _logger.OperationFailed(operationName, sw.ElapsedMilliseconds, message: result.Error.Message);
                }

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
