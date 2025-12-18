using DreamTravel.GraphDatabase.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.GraphDatabase;

public static class ModuleInstaller
{
    public static IServiceCollection InstallGraphDatabase(this IServiceCollection services)
    {
        services.AddScoped<IDriverProvider, Neo4jDriverProvider>();
        services.AddScoped<IStreetRepository, StreetRepository>();
        services.AddScoped<IIntersectionRepository, IntersectionRepository>();

        return services;
    }
}