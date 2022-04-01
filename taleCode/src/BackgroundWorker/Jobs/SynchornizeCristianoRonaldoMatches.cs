using SolTechnology.Core.Scheduler;
using SolTechnology.Core.Scheduler.ScheduleConfig;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;

namespace SolTechnology.TaleCode.BackgroundWorker.Jobs
{
    public class SynchornizeCristianoRonaldoMatches : ScheduledJob
    {
        private readonly ICommandHandler<SynchronizePlayerMatchesCommand> _handler;

        public SynchornizeCristianoRonaldoMatches(
            IScheduleConfig<SynchornizeCristianoRonaldoMatches> config,
            IServiceScopeFactory scopeFactory)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SynchronizePlayerMatchesCommand>>();
            _handler = handler;
        }

        public override async Task Execute(CancellationToken cancellationToken)
        {
            await _handler.Handle(new SynchronizePlayerMatchesCommand(44));
        }
    }
}
