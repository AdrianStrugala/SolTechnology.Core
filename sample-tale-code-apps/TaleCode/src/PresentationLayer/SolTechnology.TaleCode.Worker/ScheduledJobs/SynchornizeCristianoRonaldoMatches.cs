using MediatR;
using SolTechnology.Core.Scheduler;
using SolTechnology.Core.Scheduler.Configuration;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;

namespace SolTechnology.TaleCode.Worker.ScheduledJobs
{
    public class SynchornizeCristianoRonaldoMatches
    {
        private readonly IMediator _mediator;

        public SynchornizeCristianoRonaldoMatches(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ScheduledJob> logger)
        {
            var scope = serviceScopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            _mediator = mediator;
        }

        public async Task Execute()
        {
            await _mediator.Send(new SynchronizePlayerMatchesCommand(44));
        }
    }
}
