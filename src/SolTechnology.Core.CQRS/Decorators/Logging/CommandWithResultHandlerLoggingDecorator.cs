using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;

namespace SolTechnology.Core.CQRS.Decorators.Logging
{
    public class CommandWithResultHandlerLoggingDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    {
        private readonly ICommandHandler<TCommand, TResult> _handler;
        private readonly ILogger<ICommandHandler<TCommand, TResult>> _logger;

        public CommandWithResultHandlerLoggingDecorator(
            ICommandHandler<TCommand, TResult> handler,
            ILogger<ICommandHandler<TCommand, TResult>> logger)
        {
            _handler = handler;
            _logger = logger;
        }

        public async Task<CommandResult<TResult>> Handle(TCommand command)
        {
            if (command is ILoggedOperation loggedOperation)
            {
                using (_logger.BeginOperationScope(new KeyValuePair<string, object>(loggedOperation.LogScope.OperationIdName,
                           loggedOperation.LogScope.OperationId)))
                {
                    _logger.OperationStarted(loggedOperation.LogScope.OperationName);

                    var result = await _handler.Handle(command);

                    if (result.IsSuccess)
                    {
                        _logger.OperationSucceeded(loggedOperation.LogScope.OperationName);
                    }
                    else
                    {
                        _logger.OperationFailed(loggedOperation.LogScope.OperationName, message: result.ErrorMessage);
                    }

                    return result;
                }

            }
            else
            {
                return await _handler.Handle(command);
            }
        }
    }
}
