using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using SolTechnology.Core.Api;
using SolTechnology.Core.Api.Middlewares;
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
            services.AddSingleton<IActionResultExecutor<ObjectResult>, ResponseEnvelopeResultExecutor>();

            services.InstallCommands();

            services.AddScheduledJob<SynchornizeCristianoRonaldoMatches>(new ScheduledJobConfiguration("0 0 * * *")); //every day at midnight

            services.AddControllers();

            services.AddMessageBus()
                    .WithQueueReceiver<PlayerMatchesSynchronizedEvent, CalculatePlayerStatistics>();

        }

        public void Configure(IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                context.Request.EnableBuffering();
                await next();
            });
            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = LogEnrichmentHelper.EnrichLogs;
                options.MessageTemplate =
                    "HTTP {RequestMethod} {RequestPath} with headers {Headers} and body {Body} responded {StatusCode} in {Elapsed:0.0000} ms";
            });
            app.UseMiddleware<ExceptionHandlerMiddleware>();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
