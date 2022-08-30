
using DreamTravel.AvroConvertOnline.GenerateModel;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.AvroConvertOnline
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallAvroConvertOnline(this IServiceCollection services)
        {
            services.AddScoped<IGenerateModelHandler, GenerateModelHandler>();

            return services;
        }
    }
}
