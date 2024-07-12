using MediatR;
using SolTechnology.Core.MessageBus.Receive;
using SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics;

namespace SolTechnology.TaleCode.BackgroundWorker.EventHandlers.OnPlayerMatchesSynchronized
{
    public class CalculatePlayerStatistics : IMessageHandler<PlayerMatchesSynchronizedEvent>
    {
        private readonly IMediator _mediator;

        public CalculatePlayerStatistics(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(PlayerMatchesSynchronizedEvent message, CancellationToken cancellationToken)
        {
            var command = new CalculatePlayerStatisticsCommand(message.PlayerId);
            await _mediator.Send(command, cancellationToken);
        }

    }
}
