using DreamTravel.Identity.DatabaseData.Repository.Users;
using DreamTravel.Identity.Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Sql;

namespace DreamTravel.Identity.DatabaseData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallDatabaseData(this IServiceCollection services)
        {
            services.AddSql();

            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}
