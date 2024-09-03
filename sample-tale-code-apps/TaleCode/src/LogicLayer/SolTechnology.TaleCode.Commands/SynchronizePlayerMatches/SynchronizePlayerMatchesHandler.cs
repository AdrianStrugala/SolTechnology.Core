using MediatR;
using Quartz;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.MessageBus;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class SynchronizePlayerMatchesHandler : ICommandHandler<SynchronizePlayerMatchesCommand>, IRequestHandler<SynchronizePlayerMatchesCommand, Result>
    {
        private readonly IScheduler _scheduler;
        private readonly Func<SynchronizePlayerMatchesContext, Task<Result>> _synchronizePlayer;
        private readonly Func<SynchronizePlayerMatchesContext, Task<Result>> _calculateMatchesToSync;
        private readonly Func<SynchronizePlayerMatchesContext, Task<Result>> _synchronizeMatch;
        private readonly Func<IMessage, Task> _publishMessage;

        public SynchronizePlayerMatchesHandler(
            ISyncPlayer syncPlayer,
            IDetermineMatchesToSync determineMatchesToSync,
            ISyncMatches syncMatches,
            IMessagePublisher messagePublisher,
            ISchedulerFactory schedulerFactory)
        {
            _scheduler = schedulerFactory.GetScheduler("Scheduler-Core").GetAwaiter().GetResult();
            _synchronizePlayer = syncPlayer.Execute;
            _calculateMatchesToSync = determineMatchesToSync.Execute;
            _synchronizeMatch = syncMatches.Execute;
            _publishMessage = messagePublisher.Publish;
        }

        public async Task<Result> Handle(SynchronizePlayerMatchesCommand command, CancellationToken cancellationToken = default)
        {
            var context = new SynchronizePlayerMatchesContext(command.PlayerId);

            var result = await Chain
                .Start(context, cancellationToken)
                .Then(_synchronizePlayer)
                .Then(_calculateMatchesToSync)
                .Then(_synchronizeMatch)
                .Then(_ => _publishMessage(new PlayerMatchesSynchronizedEvent(context.PlayerId)))
            .End();

            var jobData = new JobDataMap { { "PlayerId", command.PlayerId } };
            await _scheduler.TriggerJob(new JobKey("name", "group"), jobData, cancellationToken);

            return result;
        }
    }
}
