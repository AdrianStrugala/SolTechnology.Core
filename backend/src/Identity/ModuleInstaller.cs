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
            services.AddScoped<IRegisterUser, RegisterUserHandler>();

            //Logging
            services.AddScoped<ILoginUser, LoginHandler>();

            //Change Password
            services.AddScoped<IChangePassword, ChangePasswordHandler>();

            services.InstallDatabaseData();

            return services;
        }
    }
}
