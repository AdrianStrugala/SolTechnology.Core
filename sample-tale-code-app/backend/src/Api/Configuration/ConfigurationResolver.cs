namespace DreamTravel.Api.Configuration
{
    public static partial class ConfigurationResolver
    {
        public static ApplicationConfiguration GetConfiguration(string environmentName)
        {
            return environmentName?.ToLower() switch
            {
                "demo" => GetDemoConfiguration(),
                "prod" => GetProdConfiguration(),
                _ => GetLocalConfiguration()
            };
        }
    }


}