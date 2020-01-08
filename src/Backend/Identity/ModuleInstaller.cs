using DreamTravel.DatabaseData.Configuration;
using DreamTravel.Identity.ChangePassword;
using DreamTravel.Identity.Logging;
using DreamTravel.Identity.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace DreamTravel.Identity
{
    public static class ModuleInstaller
    {
        public static IServiceCollection InstallIdentity(this IServiceCollection services)
        {

            //Registration
            services.AddScoped<IRegisterUser, RegisterUser>();

            //Logging
            services.AddScoped<ILoginUser, LoginUser>();

            //Change Password
            services.AddScoped<IChangePassword, ChangePassword.ChangePassword>();

            services.InstallDatabaseData();

            return services;
        }
    }
}
