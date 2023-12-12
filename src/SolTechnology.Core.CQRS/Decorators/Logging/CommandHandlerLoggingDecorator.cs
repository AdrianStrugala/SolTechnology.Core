using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;
using System.Diagnostics;

namespace SolTechnology.Core.CQRS.Decorators.Logging
{
    public class CommandHandlerLoggingDecorator<TCommand> : ICommandHandler<TCommand>
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

        public async Task<CommandResult> Handle(TCommand command)
        {
            if (command is ILoggableOperation loggedOperation)
            {
                using (_logger.BeginOperationScope(new KeyValuePair<string, object>(loggedOperation.LogScope.OperationIdName,
                           loggedOperation.LogScope.OperationId)))
                {
                    var sw = Stopwatch.StartNew();
                    _logger.OperationStarted(loggedOperation.LogScope.OperationName);

                    var result = await _handler.Handle(command);

                    if (result.IsSuccess)
                    {
                        _logger.OperationSucceeded(loggedOperation.LogScope.OperationName, sw.ElapsedMilliseconds);
                    }
                    else
                    {
                        _logger.OperationFailed(loggedOperation.LogScope.OperationName, sw.ElapsedMilliseconds, message: result.ErrorMessage);
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
