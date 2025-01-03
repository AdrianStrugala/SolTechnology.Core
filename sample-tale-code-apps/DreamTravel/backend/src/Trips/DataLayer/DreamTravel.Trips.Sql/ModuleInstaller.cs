﻿using DreamTravel.Trips.Sql.Repositories;
using EntityGraphQL.AspNet;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Sql;

namespace DreamTravel.Trips.Sql
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallSql(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSql();

            var sqlConfiguration = configuration.GetSection("Sql").Get<SqlConfiguration>()!;
            services.AddDbContext<DreamTripsDbContext>(options =>
                options.UseSqlServer(sqlConfiguration.ConnectionString));
                // options.UseInMemoryDatabase("DreamTravelDatabase"));

            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(sqlConfiguration.ConnectionString));
                // .UseInMemoryStorage());
                
            services.AddGraphQLSchema<DreamTripsDbContext>();

            services.AddScoped<ICityRepository, CityRepository>();

            return services;
        }
    }
}
