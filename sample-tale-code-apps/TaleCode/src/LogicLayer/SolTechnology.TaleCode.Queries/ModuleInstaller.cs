using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS.PipelineBehaviors;
using SolTechnology.TaleCode.BlobData;

namespace SolTechnology.TaleCode.PlayerRegistry.Queries
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallQueries(this IServiceCollection services)
        {
            var thisAssembly = typeof(ModuleInstaller).Assembly;

            services.InstallBlobStorage();

            services.AddMediatR(
                config =>
                {
                    config.RegisterServicesFromAssembly(thisAssembly);
                    config.AddOpenBehavior(typeof(FluentValidationPipelineBehavior<,>));
                    config.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));
                });
            services.AddValidatorsFromAssembly(thisAssembly);

            return services;
        }
    }
}
