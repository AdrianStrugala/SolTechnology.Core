namespace DreamTravel.Infrastructure.Configuration
{
    public interface IApiConfiguration
    {
        string ApiName { get; set; }
    }

    public class ApiConfiguration : IApiConfiguration
    {
        public string ApiName { get; set; }
    }
}
