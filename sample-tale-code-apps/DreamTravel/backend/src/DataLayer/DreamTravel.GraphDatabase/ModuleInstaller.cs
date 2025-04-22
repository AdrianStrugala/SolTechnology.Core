using DreamTravel.GraphDatabase.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.GraphDatabase;

public static class ModuleInstaller
{
    public static IServiceCollection InstallGraphDatabase(this IServiceCollection services)
    {
        services.AddSingleton<IDriverProvider, Neo4jDriverProvider>();
        services.AddSingleton<IStreetRepository, StreetRepository>();
        services.AddSingleton<IIntersectionRepository, IntersectionRepository>();

        return services;
    }
}