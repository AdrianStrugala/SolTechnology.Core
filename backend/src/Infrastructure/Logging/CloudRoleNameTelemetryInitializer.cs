using DreamTravel.Infrastructure.Configuration;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace DreamTravel.Infrastructure.Logging
{
    public class CloudRoleNameTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IApiConfiguration _apiConfiguration;

        public CloudRoleNameTelemetryInitializer(IApiConfiguration apiConfiguration)
        {
            _apiConfiguration = apiConfiguration;
        }

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = _apiConfiguration.ApiName;
        }
    }
}
