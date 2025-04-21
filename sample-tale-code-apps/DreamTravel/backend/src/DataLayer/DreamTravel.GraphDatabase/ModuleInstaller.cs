using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.GraphDatabase;

public static class ModuleInstaller
{
    public static IServiceCollection InstallGraphDatabase(this IServiceCollection services)
    {
        services.AddSingleton<IDriverProvider, Neo4jDriverProvider>();

        return services;
    }
}