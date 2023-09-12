using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Logging;

namespace SolTechnology.TaleCode.Infrastructure
{
    public class CommandHandlerLoggingDecorator<TCommand> : ICommandHandler<TCommand> where TCommand : ILoggedOperation
    {

        //TODO: Extract decorator to Logging
        //TODO: Remove ICommand dependency


        private readonly ICommandHandler<TCommand> _handler;
        private readonly ILogger<ICommandHandler<TCommand>> _logger;

        public CommandHandlerLoggingDecorator(ICommandHandler<TCommand> handler,
            ILogger<ICommandHandler<TCommand>> logger)
        {
            _handler = handler;
            _logger = logger;
        }

        public async Task<CommandResult> Handle(TCommand command)
        {
            using (_logger.BeginOperationScope(new KeyValuePair<string, object>(command.LogScope.OperationIdName,
                       command.LogScope.OperationId)))
            {
                _logger.OperationStarted(command.LogScope.OperationName);

                var result = await _handler.Handle(command);

                if (result.IsSuccess)
                {
                    _logger.OperationSucceeded(command.LogScope.OperationName);
                }
                else
                {
                    _logger.OperationFailed(command.LogScope.OperationName, message: result.ErrorMessage);
                }

                return result;
            }
        }
    }
}
