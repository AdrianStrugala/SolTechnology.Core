namespace DreamTravel.Bot
{
    using Configuration;
    public class ApplicationConfiguration : IDbConfiguration
    {
        public string ConnectionString { get; set; }
    }
}
