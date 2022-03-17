using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;

namespace SolTechnology.TaleCode.Infrastructure
{
    public class CommandHandlerLoggingDecorator<TCommand> : ICommandHandler<TCommand> where TCommand : ICommand
    {
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
            using (_logger.BeginOperationScope(new { command.CommandId }))
            {
                _logger.OperationStarted(command.CommandName);

                try
                {
                    await _handler.Handle(command);
                }
                catch (Exception e)
                {
                    _logger.OperationFailed(command.CommandName, e);
                    throw;
                }
            }
        }
    }
}
