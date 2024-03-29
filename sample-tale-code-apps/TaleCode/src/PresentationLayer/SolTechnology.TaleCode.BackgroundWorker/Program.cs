using Serilog;

namespace SolTechnology.TaleCode.BackgroundWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()

                        .ConfigureLogging(builder =>
                            // Optional: Apply filters to control what logs are sent to Application Insights.
                            // The following configures LogLevel Information or above to be sent to
                            // Application Insights for all categories.
                            builder
                                .AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.
                                        ApplicationInsightsLoggerProvider>
                                    ("", LogLevel.Information));
                }).UseSerilog((context, services, config) =>
                config
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.WithTraceIdentifier()
                    .Enrich.FromLogContext()
                    .WriteTo.Console(),
                    writeToProviders: true);
    }
}