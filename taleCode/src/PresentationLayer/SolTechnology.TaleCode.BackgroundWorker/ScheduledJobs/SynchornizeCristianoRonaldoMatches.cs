using SolTechnology.Core.Scheduler;
using SolTechnology.Core.Scheduler.Configuration;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;

namespace SolTechnology.TaleCode.BackgroundWorker.ScheduledJobs
{
    public class SynchornizeCristianoRonaldoMatches : ScheduledJob
    {
        private readonly ICommandHandler<SynchronizePlayerMatchesCommand> _handler;

        public SynchornizeCristianoRonaldoMatches(
            ISchedulerConfigurationProvider schedulerConfigurationProvider,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ScheduledJob> logger)
            : base(schedulerConfigurationProvider, serviceScopeFactory, logger)
        {
            var scope = serviceScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SynchronizePlayerMatchesCommand>>();
            _handler = handler;
        }

        public override async Task Execute()
        {
            await _handler.Handle(new SynchronizePlayerMatchesCommand(44));
        }
    }
}
