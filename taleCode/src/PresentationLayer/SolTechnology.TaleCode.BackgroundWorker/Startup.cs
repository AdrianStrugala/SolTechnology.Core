using SolTechnology.Core.MessageBus;
using SolTechnology.Core.Scheduler;
using SolTechnology.Core.Scheduler.Configuration;
using SolTechnology.TaleCode.BackgroundWorker.EventHandlers.OnPlayerMatchesSynchronized;
using SolTechnology.TaleCode.BackgroundWorker.ScheduledJobs;
using SolTechnology.TaleCode.PlayerRegistry.Commands;

namespace SolTechnology.TaleCode.BackgroundWorker
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddLogging(c =>
                c.AddConsole()
                    .AddApplicationInsights());
            services.AddApplicationInsightsTelemetry();

            services.InstallCommands();

            services.AddScheduledJob<SynchornizeCristianoRonaldoMatches>(new ScheduledJobConfiguration("0 0 * * *")); //every day at midnight

            services.AddControllers();

            services.AddMessageBus()
                .WithQueueReceiver<PlayerMatchesSynchronizedEvent, CalculatePlayerStatistics>();

        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
