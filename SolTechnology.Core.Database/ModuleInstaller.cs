using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Database.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SolTechnology.Core.Database
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddSql(this IServiceCollection services, IConfiguration configuration, SqlConfiguration sqlConfiguration = null)
        {

            services
                .AddOptions<SqlConfiguration>()
                .Configure<IConfiguration>((config, configuration) =>
           {
               if (sqlConfiguration == null)
               {
                   sqlConfiguration = configuration.GetRequiredSection("Configuration:Sql").Get<SqlConfiguration>();
               }
               config = sqlConfiguration;
           });

            return services;
        }
    }
}
