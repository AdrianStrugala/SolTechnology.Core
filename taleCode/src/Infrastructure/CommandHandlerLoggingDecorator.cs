using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;

namespace SolTechnology.TaleCode.Infrastructure
{
    public class CommandHandlerLoggingDecorator<TCommand> : ICommandHandler<TCommand> where TCommand : ILoggedOperation, ICommand
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

        public async Task Handle(TCommand command)
        {
            using (_logger.BeginOperationScope(new KeyValuePair<string, object>(command.LogScope.OperationIdName, command.LogScope.OperationId)))
            {
                _logger.OperationStarted(command.LogScope.OperationName);

                try
                {
                    await _handler.Handle(command);
                }
                catch (Exception e)
                {
                    _logger.OperationFailed(command.LogScope.OperationName, e);
                    throw;
                }
            }
        }
    }
}
