using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.TaleCode.StaticData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddStaticData(this IServiceCollection services)
        {
            services.AddTransient<IPlayerIdProvider, PlayerIdProvider>();

            return services;
        }
    }
}
