﻿using Microsoft.Extensions.DependencyInjection;
using SolTechnology.TaleCode.StaticData.PlayerId;

namespace SolTechnology.TaleCode.StaticData
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallStaticData(this IServiceCollection services)
        {
            services.AddTransient<IPlayerExternalIdsProvider, PlayerExternalIdsProvider>();

            return services;
        }
    }
}
