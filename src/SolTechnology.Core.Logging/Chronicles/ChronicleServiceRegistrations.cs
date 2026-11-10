using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using SolTechnology.Core.Logging.Chronicles.FlightRecorder;
using SolTechnology.Core.Logging.Chronicles.SQL;

namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>
/// The registrations behind <c>AddSolChronicle</c>. Services are registered once; the returned
/// options builder lets every caller apply its own <c>Configure</c> or <c>Bind</c>, whatever the call order.
/// </summary>
internal static class ChronicleServiceRegistrations
{
    internal static OptionsBuilder<ChronicleOptions> Register(IServiceCollection services)
    {
        var options = services.AddOptions<ChronicleOptions>();
        if (services.Any(d => d.ServiceType == typeof(IChronicle)))
        {
            return options;
        }

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IChronicle, ChronicleRecorder>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IChronicleRootListener, ChronicleReporter>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<ChronicleOptions>, ChronicleOptionsValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ChronicleFlightRecorderProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<LoggerFilterOptions>, ChronicleFlightRecorderFilterSetup>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ChronicleSQLClientSubscription>());
        return options.ValidateOnStart();
    }
}
